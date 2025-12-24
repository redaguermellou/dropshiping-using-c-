using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la catégorie est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Catégorie parente")]
        public int? ParentCategoryId { get; set; }

        [Display(Name = "Icône")]
        [StringLength(50)]
        public string Icon { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ParentCategoryId")]
        public virtual Category ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }

        // Calculated properties
        [NotMapped]
        [Display(Name = "Nombre de produits")]
        public int ProductCount => Products?.Count ?? 0;

        [NotMapped]
        public bool HasParent => ParentCategoryId.HasValue;

        [NotMapped]
        public string FullName => ParentCategory != null ? $"{ParentCategory.Name} > {Name}" : Name;
    }
}
