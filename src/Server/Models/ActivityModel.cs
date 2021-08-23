﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RestIdentity.Server.Models;

public class ActivityModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; }

    [MaxLength(100)]
    public string IpAddress { get; set; }

    [MaxLength(100)]
    public string Location { get; set; }

    [MaxLength(100)]
    public string OperationgSystem { get; set; }

    public DateTime Date { get; set; }
}
