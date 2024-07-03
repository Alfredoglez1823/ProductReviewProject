using System;
using System.Collections.Generic;

namespace ProductReviewAPI.Models;

public partial class EmailVerification
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public int Code { get; set; }
}
