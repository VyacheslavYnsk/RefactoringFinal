using System.ComponentModel.DataAnnotations;
public enum AgeRating
{
    [Display(Name = "0+")]
    Zero = 0,

    [Display(Name = "8+")]
    Eight = 8,

    [Display(Name = "12+")]
    Twelve = 12,

    [Display(Name = "16+")]
    Sixteen = 16,

    [Display(Name = "18+")]
    Eighteen = 18
}