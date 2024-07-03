using System;
using System.Collections.Generic;

namespace ProductReviewAPI.Models;

public partial class ProductComment
{
    public int Id { get; set; }

    public string Product { get; set; } = null!;

    public string? Comment { get; set; }

    public string? Prediction { get; set; }
}
