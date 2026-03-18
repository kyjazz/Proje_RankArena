using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Helpers;
using RankArena.Models.Entities;

namespace RankArena.Controllers;

public class PlayController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public PlayController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Play/Start?slug=...&mode=bracket&value=16
    [HttpGet]
    public async Task<IActionResult> Start(string slug, string mode, int? value)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest("slug boş olamaz.");

        if (string.IsNullOrWhiteSpace(mode))
            return BadRequest("mode boş olamaz.");

        var tournament = await _db.Tournaments
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Slug == slug && t.IsPublished);

        if (tournament == null)
            return NotFound();

        var activeCount = tournament.Items.Count(i => i.IsActive);

        var parsedMode = ParseMode(mode);
        if (parsedMode == null)
            return BadRequest("Geçersiz mode. (bracket/blind/tier)");

        var error = ValidateRequest(parsedMode.Value, value, activeCount);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Details", "Tournaments", new { slug });
        }

        var sessionKey = SessionKeyHelper.GetOrCreate(HttpContext);

        string? userId = null;
        if (User?.Identity?.IsAuthenticated == true)
            userId = _userManager.GetUserId(User);

        var fifteenMinAgo = DateTime.UtcNow.AddMinutes(-15);

        // TierList'te TotalSlots null olacak
        var desiredTotalSlots = (parsedMode.Value == GameMode.TierList) ? null : value;

        var existingRun = await _db.Runs
            .Where(r =>
                r.TournamentId == tournament.Id &&
                r.SessionKey == sessionKey &&
                r.UserId == userId &&
                r.Mode == parsedMode.Value &&
                r.FinishedAt == null &&
                r.StartedAt >= fifteenMinAgo &&
                r.TotalSlots == desiredTotalSlots
            )
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        Run run;

        if (existingRun != null)
        {
            run = existingRun;

            // ✅ Bracket ise ve match yoksa üret
            if (run.Mode == GameMode.Bracket)
            {
                var hasAnyMatch = await _db.BracketMatches.AnyAsync(m => m.RunId == run.Id);
                if (!hasAnyMatch)
                    await CreateBracketMatchesAsync(run.Id);
            }

            // ✅ Blind ise pool/slot yoksa üret
            if (run.Mode == GameMode.BlindRank)
            {
                var hasPool = await _db.BlindRunItems.AnyAsync(x => x.RunId == run.Id);
                if (!hasPool)
                    await CreateBlindPoolAsync(run.Id);

                var hasSlots = await _db.BlindSlots.AnyAsync(x => x.RunId == run.Id);
                if (!hasSlots)
                    await CreateBlindSlotsAsync(run.Id);
            }

            // ✅ TierList ise pool yoksa üret
            if (run.Mode == GameMode.TierList)
            {
                var hasTierPool = await _db.TierRunItems.AnyAsync(x => x.RunId == run.Id);
                if (!hasTierPool)
                    await CreateTierPoolAsync(run.Id);
            }
        }
        else
        {
            run = new Run
            {
                TournamentId = tournament.Id,
                Mode = parsedMode.Value,
                UserId = userId,
                SessionKey = sessionKey,
                StartedAt = DateTime.UtcNow,
                TotalSlots = desiredTotalSlots,
                WinnerItemId = null
            };

            _db.Runs.Add(run);
            await _db.SaveChangesAsync();

            if (run.Mode == GameMode.Bracket)
                await CreateBracketMatchesAsync(run.Id);

            if (run.Mode == GameMode.BlindRank)
            {
                await CreateBlindPoolAsync(run.Id);
                await CreateBlindSlotsAsync(run.Id);
            }

            if (run.Mode == GameMode.TierList)
                await CreateTierPoolAsync(run.Id);
        }

        return run.Mode switch
        {
            GameMode.Bracket => RedirectToAction(nameof(Bracket), new { runId = run.Id }),
            GameMode.BlindRank => RedirectToAction(nameof(Blind), new { runId = run.Id }),
            GameMode.TierList => RedirectToAction(nameof(Tier), new { runId = run.Id }),
            _ => RedirectToAction("Details", "Tournaments", new { slug })
        };
    }

    // =========================================================
    // ✅ BRACKET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Bracket(int runId)
    {
        var run = await _db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return NotFound();
        if (run.Mode != GameMode.Bracket) return BadRequest("Bu run Bracket değil.");

        if (run.FinishedAt != null || run.WinnerItemId != null)
            return RedirectToAction(nameof(BracketResult), new { runId });

        var next = await _db.BracketMatches
            .Include(m => m.LeftItem)
            .Include(m => m.RightItem)
            .Where(m => m.RunId == runId && m.WinnerItemId == null)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.MatchNumber)
            .FirstOrDefaultAsync();

        if (next == null)
            return RedirectToAction(nameof(BracketResult), new { runId });

        if (next.LeftItemId == null || next.RightItemId == null)
        {
            TempData["Error"] = "Eşleşme hazır değil (eksik item).";
            return RedirectToAction(nameof(BracketResult), new { runId });
        }

        var round = next.Round;

        var totalThisRound = await _db.BracketMatches
            .Where(m => m.RunId == runId && m.Round == round)
            .CountAsync();

        var remainingThisRound = await _db.BracketMatches
            .Where(m => m.RunId == runId && m.Round == round && m.WinnerItemId == null)
            .CountAsync();

        ViewBag.TotalThisRound = totalThisRound;
        ViewBag.RemainingThisRound = remainingThisRound;

        var totalMatches = await _db.BracketMatches
            .Where(m => m.RunId == runId)
            .CountAsync();

        var completedMatches = await _db.BracketMatches
            .Where(m => m.RunId == runId && m.WinnerItemId != null)
            .CountAsync();

        var remainingMatches = totalMatches - completedMatches;

        ViewBag.TotalMatches = totalMatches;
        ViewBag.CompletedMatches = completedMatches;
        ViewBag.RemainingMatches = remainingMatches;

        ViewBag.ProgressPercent = totalMatches == 0
            ? 0
            : (int)Math.Round(completedMatches * 100.0 / totalMatches);

        var totalRounds = (int)Math.Log2(run.TotalSlots ?? 0);
        ViewBag.RoundName = GetRoundName(next.Round, totalRounds);
        ViewBag.TotalSlots = run.TotalSlots ?? 0;  // ✅ YENİ: Quizei tarzı tur pill için

        return View(next);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BracketVote(int runId, int matchId, int selectedItemId)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return NotFound();
        if (run.Mode != GameMode.Bracket) return BadRequest("Bu run Bracket değil.");
        if (run.FinishedAt != null) return RedirectToAction(nameof(BracketResult), new { runId });

        var match = await _db.BracketMatches.FirstOrDefaultAsync(m => m.Id == matchId && m.RunId == runId);
        if (match == null) return NotFound();

        if (match.WinnerItemId != null)
            return RedirectToAction(nameof(Bracket), new { runId });

        if (selectedItemId != match.LeftItemId && selectedItemId != match.RightItemId)
            return BadRequest("Seçim bu eşleşmeye ait değil.");

        var sessionKey = SessionKeyHelper.GetOrCreate(HttpContext);

        string? userId = null;
        if (User?.Identity?.IsAuthenticated == true)
            userId = _userManager.GetUserId(User);

        match.WinnerItemId = selectedItemId;
        match.CompletedAt = DateTime.UtcNow;

        _db.BracketVotes.Add(new BracketVote
        {
            RunId = runId,
            MatchId = matchId,
            SelectedItemId = selectedItemId,
            UserId = userId,
            SessionKey = sessionKey,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // ✅ Bu turun tüm maçları bitti mi kontrol et
        var currentRound = match.Round;
        var totalSlots = run.TotalSlots ?? 0;
        var totalRounds = (int)Math.Log2(totalSlots);

        // Final turuysa → turnuva bitti
        if (currentRound == totalRounds)
        {
            run.WinnerItemId = match.WinnerItemId;
            run.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(BracketResult), new { runId });
        }

        // Bu turdaki tüm maçlar tamamlandı mı?
        var unfinishedInRound = await _db.BracketMatches
            .CountAsync(m => m.RunId == runId && m.Round == currentRound && m.WinnerItemId == null);

        if (unfinishedInRound == 0)
        {
            // ✅ Tur bitti! Kazananları topla ve RASTGELE karıştırarak sonraki tura yerleştir
            await ShuffleWinnersToNextRoundAsync(run, currentRound);
            await _db.SaveChangesAsync();
        }

        if (run.FinishedAt != null || run.WinnerItemId != null)
            return RedirectToAction(nameof(BracketResult), new { runId });

        return RedirectToAction(nameof(Bracket), new { runId });
    }

    [HttpGet]
    public async Task<IActionResult> BracketResult(int runId)
    {
        var run = await _db.Runs
            .Include(r => r.Tournament)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();
        if (run.Mode != GameMode.Bracket) return BadRequest("Bu run Bracket değil.");

        TournamentItem? winner = null;
        if (run.WinnerItemId.HasValue)
        {
            winner = await _db.TournamentItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == run.WinnerItemId.Value);
        }

        ViewBag.Winner = winner;
        return View(run);
    }

    // =========================================================
    // ✅ BLIND RANK
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Blind(int runId)
    {
        var run = await _db.Runs
            .Include(r => r.Tournament)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();
        if (run.Mode != GameMode.BlindRank) return BadRequest("Bu run BlindRank değil.");

        ViewBag.Slug = run.Tournament?.Slug;

        if (run.FinishedAt != null || run.WinnerItemId != null)
            return RedirectToAction(nameof(BlindResult), new { runId });

        var hasPool = await _db.BlindRunItems.AnyAsync(x => x.RunId == runId);
        if (!hasPool)
            await CreateBlindPoolAsync(runId);

        var hasSlots = await _db.BlindSlots.AnyAsync(x => x.RunId == runId);
        if (!hasSlots)
            await CreateBlindSlotsAsync(runId);

        var slots = await _db.BlindSlots
            .Include(s => s.TournamentItem)
            .Where(s => s.RunId == runId)
            .OrderBy(s => s.Position)
            .AsNoTracking()
            .ToListAsync();

        var pool = await _db.BlindRunItems
            .Include(x => x.TournamentItem)
            .Where(x => x.RunId == runId)
            .OrderBy(x => x.Sequence)
            .AsNoTracking()
            .ToListAsync();

        var next = pool.FirstOrDefault(p =>
            !slots.Any(s => s.TournamentItemId == p.TournamentItemId));

        if (next == null)
            return RedirectToAction(nameof(BlindResult), new { runId });

        var total = slots.Count;
        var filled = slots.Count(s => s.TournamentItemId != null);

        ViewBag.Slots = slots;
        ViewBag.TotalSlots = total;
        ViewBag.Completed = filled;
        ViewBag.Remaining = total - filled;
        ViewBag.Percent = total == 0 ? 0 : (int)Math.Round((filled * 100.0) / total);

        return View(next);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlindPlace(int runId, int blindRunItemId, int position)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return NotFound();
        if (run.Mode != GameMode.BlindRank) return BadRequest("Bu run BlindRank değil.");
        if (run.FinishedAt != null) return RedirectToAction(nameof(BlindResult), new { runId });

        var bri = await _db.BlindRunItems
            .Include(x => x.TournamentItem)
            .FirstOrDefaultAsync(x => x.Id == blindRunItemId && x.RunId == runId);

        if (bri == null) return NotFound();

        var slot = await _db.BlindSlots.FirstOrDefaultAsync(s => s.RunId == runId && s.Position == position);
        if (slot == null) return BadRequest("Geçersiz slot.");
        if (slot.TournamentItemId != null) return BadRequest("Bu slot dolu.");

        var alreadyPlaced = await _db.BlindSlots.AnyAsync(s => s.RunId == runId && s.TournamentItemId == bri.TournamentItemId);
        if (alreadyPlaced)
            return RedirectToAction(nameof(Blind), new { runId });

        var sessionKey = SessionKeyHelper.GetOrCreate(HttpContext);
        string? userId = null;
        if (User?.Identity?.IsAuthenticated == true)
            userId = _userManager.GetUserId(User);

        slot.TournamentItemId = bri.TournamentItemId;
        slot.FilledAt = DateTime.UtcNow;

        _db.BlindPicks.Add(new BlindPick
        {
            RunId = runId,
            TournamentItemId = bri.TournamentItemId,
            Position = position,
            UserId = userId,
            SessionKey = sessionKey,
            CreatedAt = DateTime.UtcNow
        });

        var total = await _db.BlindSlots.CountAsync(s => s.RunId == runId);
        var filled = await _db.BlindSlots.CountAsync(s => s.RunId == runId && s.TournamentItemId != null);

        if (filled >= total && total > 0)
        {
            var winnerSlot = await _db.BlindSlots.FirstOrDefaultAsync(s => s.RunId == runId && s.Position == 1);
            run.WinnerItemId = winnerSlot?.TournamentItemId;
            run.FinishedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        if (run.FinishedAt != null)
            return RedirectToAction(nameof(BlindResult), new { runId });

        return RedirectToAction(nameof(Blind), new { runId });
    }

    [HttpGet]
    public async Task<IActionResult> BlindResult(int runId)
    {
        var run = await _db.Runs
            .Include(r => r.Tournament)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();
        if (run.Mode != GameMode.BlindRank) return BadRequest("Bu run BlindRank değil.");

        ViewBag.Slug = run.Tournament?.Slug;

        var slots = await _db.BlindSlots
            .Include(s => s.TournamentItem)
            .Where(s => s.RunId == runId)
            .OrderBy(s => s.Position)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Slots = slots;

        TournamentItem? winner = null;
        if (run.WinnerItemId.HasValue)
            winner = await _db.TournamentItems.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == run.WinnerItemId.Value);

        ViewBag.Winner = winner;

        return View(run);
    }

    // =========================================================
    // ✅ TIER LIST (GERÇEK OYNANIŞ) ✅ GÜNCEL + GEÇ DESTEĞİ
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Tier(int runId)
    {
        var run = await _db.Runs
            .Include(r => r.Tournament)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();
        if (run.Mode != GameMode.TierList) return BadRequest("Bu run TierList değil.");

        ViewBag.Slug = run.Tournament?.Slug;

        if (run.FinishedAt != null)
            return RedirectToAction(nameof(TierResult), new { runId });

        // pool yoksa üret
        var hasPool = await _db.TierRunItems.AnyAsync(x => x.RunId == runId);
        if (!hasPool)
            await CreateTierPoolAsync(runId);

        // placements (TierSlot)
        var placed = await _db.TierSlots
            .Include(x => x.TournamentItem)
            .Where(x => x.RunId == runId)
            .AsNoTracking()
            .ToListAsync();

        // pool (TierRunItem)
        var pool = await _db.TierRunItems
            .Include(x => x.TournamentItem)
            .Where(x => x.RunId == runId)
            .OrderBy(x => x.Sequence)
            .AsNoTracking()
            .ToListAsync();

        // ✅ skip edilenler
        var skippedIds = await _db.TierSkips
            .Where(x => x.RunId == runId)
            .Select(x => x.TournamentItemId)
            .ToListAsync();

        ViewBag.SkipCount = skippedIds.Count;

        // ✅ sıradaki (yerleştirilmemiş VE skip edilmemiş)
        var next = pool.FirstOrDefault(p =>
            !placed.Any(s => s.TournamentItemId == p.TournamentItemId)
            && !skippedIds.Contains(p.TournamentItemId));

        if (next == null)
            return RedirectToAction(nameof(TierResult), new { runId });

        // ✅ progress (placed + skipped)
        var total = pool.Count;
        var done = placed.Count + skippedIds.Count;

        ViewBag.Total = total;
        ViewBag.Done = done;
        ViewBag.Remaining = total - done;
        ViewBag.Percent = total == 0 ? 0 : (int)Math.Round((done * 100.0) / total);

        ViewBag.Placed = placed;

        // ✅ Sürükle & Bırak için henüz yerleştirilmemiş VE skip edilmemiş item'ler
        var unplaced = pool
            .Where(p => !placed.Any(s => s.TournamentItemId == p.TournamentItemId)
                     && !skippedIds.Contains(p.TournamentItemId))
            .ToList();
        ViewBag.UnplacedPool = unplaced;

        // ✅ Board chip'lerinin TierRunItemId'sini bulmak için tüm pool
        ViewBag.AllPool = pool;

        return View(next); // Views/Play/Tier.cshtml
    }

    // ✅ TierPlace: aynı item varsa UPDATE (tier değiştir), yoksa INSERT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TierPlace(int runId, int tierRunItemId, Tier tier)
    {
        var run = await _db.Runs
            .Include(r => r.Tournament)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();
        if (run.Mode != GameMode.TierList) return BadRequest("Bu run TierList değil.");
        if (run.FinishedAt != null) return RedirectToAction(nameof(TierResult), new { runId });

        var tri = await _db.TierRunItems
            .Include(x => x.TournamentItem)
            .FirstOrDefaultAsync(x => x.Id == tierRunItemId && x.RunId == runId);

        if (tri == null) return NotFound();

        // kimlik
        var sessionKey = SessionKeyHelper.GetOrCreate(HttpContext);
        string? userId = null;
        if (User?.Identity?.IsAuthenticated == true)
            userId = _userManager.GetUserId(User);

        // ✅ varsa değiştir (UPDATE)
        var existingSlot = await _db.TierSlots
            .FirstOrDefaultAsync(x => x.RunId == runId && x.TournamentItemId == tri.TournamentItemId);

        if (existingSlot == null)
        {
            _db.TierSlots.Add(new TierSlot
            {
                RunId = runId,
                TournamentItemId = tri.TournamentItemId,
                Tier = tier,
                PlacedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingSlot.Tier = tier;
            existingSlot.PlacedAt = DateTime.UtcNow;
        }

        // log
        _db.TierPicks.Add(new TierPick
        {
            RunId = runId,
            TournamentItemId = tri.TournamentItemId,
            Tier = tier,
            UserId = userId,
            SessionKey = sessionKey,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // ✅ bitti mi? (placed + skipped >= total)
        var total = await _db.TierRunItems.CountAsync(x => x.RunId == runId);
        var placed = await _db.TierSlots.CountAsync(x => x.RunId == runId);
        var skipped = await _db.TierSkips.CountAsync(x => x.RunId == runId);

        if (total > 0 && (placed + skipped) >= total)
        {
            run.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(TierResult), new { runId });
        }

        return RedirectToAction(nameof(Tier), new { runId });
    }

    // ✅ Undo: son seçilen item'i TierSlots'tan kaldır (tekrar seçilebilir)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TierUndo(int runId)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return NotFound();
        if (run.Mode != GameMode.TierList) return BadRequest("Bu run TierList değil.");
        if (run.FinishedAt != null) return RedirectToAction(nameof(TierResult), new { runId });

        var sessionKey = SessionKeyHelper.GetOrCreate(HttpContext);
        string? userId = null;
        if (User?.Identity?.IsAuthenticated == true)
            userId = _userManager.GetUserId(User);

        var lastPick = await _db.TierPicks
            .Where(p => p.RunId == runId && p.SessionKey == sessionKey && p.UserId == userId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (lastPick == null)
            return RedirectToAction(nameof(Tier), new { runId });

        var slot = await _db.TierSlots
            .FirstOrDefaultAsync(s => s.RunId == runId && s.TournamentItemId == lastPick.TournamentItemId);

        if (slot != null)
            _db.TierSlots.Remove(slot);

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Tier), new { runId });
    }

    // ✅ GEÇ: item'ı pas geç, tekrar gösterilmez
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TierSkip(int runId, int tierRunItemId)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return NotFound();
        if (run.Mode != GameMode.TierList) return BadRequest("Bu run TierList değil.");
        if (run.FinishedAt != null) return RedirectToAction(nameof(TierResult), new { runId });

        var tri = await _db.TierRunItems
            .FirstOrDefaultAsync(x => x.Id == tierRunItemId && x.RunId == runId);
        if (tri == null) return NotFound();

        // zaten skip edilmiş mi?
        var alreadySkipped = await _db.TierSkips
            .AnyAsync(x => x.RunId == runId && x.TournamentItemId == tri.TournamentItemId);
        if (alreadySkipped)
            return RedirectToAction(nameof(Tier), new { runId });

        var sessionKey = SessionKeyHelper.GetOrCreate(HttpContext);
        string? userId = null;
        if (User?.Identity?.IsAuthenticated == true)
            userId = _userManager.GetUserId(User);

        _db.TierSkips.Add(new TierSkip
        {
            RunId = runId,
            TournamentItemId = tri.TournamentItemId,
            UserId = userId,
            SessionKey = sessionKey,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // ✅ tüm item'lar yerleştirildi veya skip edildi mi?
        var totalPool = await _db.TierRunItems.CountAsync(x => x.RunId == runId);
        var placedCount = await _db.TierSlots.CountAsync(x => x.RunId == runId);
        var skippedCount = await _db.TierSkips.CountAsync(x => x.RunId == runId);

        if (totalPool > 0 && (placedCount + skippedCount) >= totalPool)
        {
            run.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(TierResult), new { runId });
        }

        return RedirectToAction(nameof(Tier), new { runId });
    }

    [HttpGet]
    public async Task<IActionResult> TierResult(int runId)
    {
        var run = await _db.Runs
            .Include(r => r.Tournament)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();
        if (run.Mode != GameMode.TierList) return BadRequest("Bu run TierList değil.");

        ViewBag.Slug = run.Tournament?.Slug;

        var placements = await _db.TierSlots
            .Include(x => x.TournamentItem)
            .Where(x => x.RunId == runId)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Placements = placements;

        // ✅ Skip sayısı
        var skipCount = await _db.TierSkips.CountAsync(x => x.RunId == runId);
        ViewBag.SkipCount = skipCount;

        return View(run); // Views/Play/TierResult.cshtml
    }

    // =========================================================
    // ✅ BRACKET HELPERS
    // =========================================================
    private async Task CreateBracketMatchesAsync(int runId)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) throw new Exception("Run bulunamadı.");

        var tournament = await _db.Tournaments
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == run.TournamentId);

        if (tournament == null) throw new Exception("Tournament bulunamadı.");

        var totalSlots = run.TotalSlots ?? 0;
        if (totalSlots is not (8 or 16 or 32 or 64))
            throw new Exception("Bracket TotalSlots 8/16/32/64 olmalı.");

        var activeIds = tournament.Items
            .Where(i => i.IsActive)
            .Select(i => i.Id)
            .ToList();

        var rng = new Random();
        activeIds = activeIds.OrderBy(_ => rng.Next()).ToList();

        var picked = activeIds.Take(totalSlots).ToList();

        var rounds = (int)Math.Log2(totalSlots);
        var matches = new List<BracketMatch>();

        for (int round = 1; round <= rounds; round++)
        {
            var matchCount = totalSlots / (int)Math.Pow(2, round);
            for (int m = 1; m <= matchCount; m++)
            {
                matches.Add(new BracketMatch
                {
                    RunId = runId,
                    Round = round,
                    MatchNumber = m
                });
            }
        }

        // ✅ Sadece 1. tur item'larını yerleştir (rastgele seçildi zaten)
        for (int i = 0; i < totalSlots / 2; i++)
        {
            var match = matches.First(x => x.Round == 1 && x.MatchNumber == (i + 1));
            match.LeftItemId = picked[i * 2];
            match.RightItemId = picked[i * 2 + 1];
        }

        // ✅ 2. tur ve sonrası BOŞ kalacak, tur bitince ShuffleWinnersToNextRoundAsync dolduracak

        _db.BracketMatches.AddRange(matches);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// ✅ YENİ: Bir tur tamamen bittiğinde, o turun kazananlarını RASTGELE karıştırıp
    /// sonraki turun maçlarına yerleştirir. Böylece 2. tur ve sonrasında eşleşmeler
    /// her zaman rastgele olur (Quizei tarzı).
    /// </summary>
    private async Task ShuffleWinnersToNextRoundAsync(Run run, int completedRound)
    {
        var totalSlots = run.TotalSlots ?? 0;
        var totalRounds = (int)Math.Log2(totalSlots);

        // Final turuysa shuffle'a gerek yok
        if (completedRound >= totalRounds)
            return;

        // Bu turun tamamlanmış maçlarından kazananları topla
        var completedMatches = await _db.BracketMatches
            .Where(m => m.RunId == run.Id && m.Round == completedRound && m.WinnerItemId != null)
            .OrderBy(m => m.MatchNumber)
            .ToListAsync();

        var winnerIds = completedMatches
            .Select(m => m.WinnerItemId!.Value)
            .ToList();

        // ✅ Kazananları RASTGELE karıştır
        var rng = new Random();
        winnerIds = winnerIds.OrderBy(_ => rng.Next()).ToList();

        // Sonraki turun maçlarını al
        var nextRound = completedRound + 1;
        var nextMatches = await _db.BracketMatches
            .Where(m => m.RunId == run.Id && m.Round == nextRound)
            .OrderBy(m => m.MatchNumber)
            .ToListAsync();

        // Karıştırılmış kazananları ikişer ikişer sonraki tur maçlarına yerleştir
        for (int i = 0; i < nextMatches.Count; i++)
        {
            nextMatches[i].LeftItemId = winnerIds[i * 2];
            nextMatches[i].RightItemId = winnerIds[i * 2 + 1];
        }
    }

    private static string GetRoundName(int round, int totalRounds)
    {
        if (totalRounds <= 0) return $"Round {round}";
        if (round == totalRounds) return "Final";
        if (round == totalRounds - 1) return "Yarı Final";
        if (round == totalRounds - 2) return "Çeyrek Final";

        var remainingRoundsBeforeFinal = totalRounds - round;
        var stage = (int)Math.Pow(2, remainingRoundsBeforeFinal + 1);
        return $"Son {stage}";
    }

    // =========================================================
    // ✅ BLIND HELPERS
    // =========================================================
    private async Task CreateBlindPoolAsync(int runId)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) throw new Exception("Run bulunamadı.");

        var tournament = await _db.Tournaments
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == run.TournamentId);

        if (tournament == null) throw new Exception("Tournament bulunamadı.");

        var slots = run.TotalSlots ?? 0;
        if (slots is not (5 or 10))
            throw new Exception("BlindRank TotalSlots 5/10 olmalı.");

        var activeIds = tournament.Items
            .Where(i => i.IsActive)
            .Select(i => i.Id)
            .ToList();

        if (activeIds.Count < slots)
            throw new Exception($"Yeterli aktif item yok. Aktif: {activeIds.Count}, İstenen: {slots}");

        var rng = new Random();
        var picked = activeIds.OrderBy(_ => rng.Next()).Take(slots).ToList();

        var list = new List<BlindRunItem>();
        for (int i = 0; i < picked.Count; i++)
        {
            list.Add(new BlindRunItem
            {
                RunId = runId,
                TournamentItemId = picked[i],
                Sequence = i + 1
            });
        }

        _db.BlindRunItems.AddRange(list);
        await _db.SaveChangesAsync();
    }

    private async Task CreateBlindSlotsAsync(int runId)
    {
        var run = await _db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) throw new Exception("Run bulunamadı.");

        var slots = run.TotalSlots ?? 0;
        if (slots is not (5 or 10))
            throw new Exception("BlindRank TotalSlots 5/10 olmalı.");

        var list = new List<BlindSlot>();
        for (int pos = 1; pos <= slots; pos++)
        {
            list.Add(new BlindSlot
            {
                RunId = runId,
                Position = pos
            });
        }

        _db.BlindSlots.AddRange(list);
        await _db.SaveChangesAsync();
    }

    // =========================================================
    // ✅ TIER HELPERS
    // =========================================================
    private async Task CreateTierPoolAsync(int runId)
    {
        var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) throw new Exception("Run bulunamadı.");

        var tournament = await _db.Tournaments
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == run.TournamentId);

        if (tournament == null) throw new Exception("Tournament bulunamadı.");

        var activeIds = tournament.Items
            .Where(i => i.IsActive)
            .Select(i => i.Id)
            .ToList();

        if (activeIds.Count < 2)
            throw new Exception("Tier List için en az 2 aktif item olmalı.");

        var rng = new Random();
        activeIds = activeIds.OrderBy(_ => rng.Next()).ToList();

        var list = new List<TierRunItem>();
        for (int i = 0; i < activeIds.Count; i++)
        {
            list.Add(new TierRunItem
            {
                RunId = runId,
                TournamentItemId = activeIds[i],
                Sequence = i + 1
            });
        }

        _db.TierRunItems.AddRange(list);
        await _db.SaveChangesAsync();
    }

    // =========================================================
    // Mode Parse + Validation
    // =========================================================
    private static GameMode? ParseMode(string mode)
    {
        var m = mode.Trim().ToLowerInvariant();

        return m switch
        {
            "bracket" => GameMode.Bracket,
            "blind" => GameMode.BlindRank,
            "tier" => GameMode.TierList,
            _ => null
        };
    }

    private static string? ValidateRequest(GameMode mode, int? value, int activeCount)
    {
        if (mode == GameMode.Bracket)
        {
            if (value is not (8 or 16 or 32 or 64))
                return "Bracket için tur değeri 8/16/32/64 olmalı.";

            if (activeCount < value)
                return $"Bu tur için yeterli aktif item yok. Aktif: {activeCount}, İstenen: {value}.";
        }

        if (mode == GameMode.BlindRank)
        {
            if (value is not (5 or 10))
                return "Kör sıralama için slot sayısı 5 veya 10 olmalı.";

            if (activeCount < value)
                return $"Kör sıralama için yeterli aktif item yok. Aktif: {activeCount}, İstenen: {value}.";
        }

        if (mode == GameMode.TierList)
        {
            if (activeCount < 2)
                return "Tier List için en az 2 aktif item olmalı.";
        }

        return null;
    }
}