using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("log_id")]
    public int LogId { get; set; }

    [Column("entity_name")]
    public string EntityName { get; set; } = "";

    [Column("entity_id")]
    public string? EntityId { get; set; }

    [Column("action")]
    public string Action { get; set; } = "";

    [Column("changed_fields")]
    public string? ChangedFields { get; set; }

    [Column("old_values")]
    public string? OldValues { get; set; }

    [Column("new_values")]
    public string? NewValues { get; set; }

    [Column("performed_by")]
    public string? PerformedBy { get; set; }

    [Column("details")]
    public string? Details { get; set; }

    [Column("timestamp")]
    public string Timestamp { get; set; } = "";
}
