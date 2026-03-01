using System;
using System.Collections.Generic;

namespace RankArena.Models.Entities
{
    public class Tournament
    {
        public int Id { get; set; }

        // -----------------------------
        // Temel Bilgiler
        // -----------------------------
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }

        // -----------------------------
        // Kategori (opsiyonel)
        // -----------------------------
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // -----------------------------
        // Oluşturan Kullanıcı
        // -----------------------------
        public string? CreatedByUserId { get; set; }

        // -----------------------------
        // Yayın Akışı
        // -----------------------------
        public bool IsPublished { get; set; } = false;

        // -----------------------------
        // Tarihler
        // -----------------------------
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // -----------------------------
        // Navigation
        // -----------------------------
        public List<TournamentItem> Items { get; set; } = new();
        public List<TournamentComment> Comments { get; set; } = new();
    }
}