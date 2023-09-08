using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Build.Evaluation;
using System;
using System.ComponentModel.DataAnnotations;

namespace Chrysalis.Models
{
    public class Invite
    {
        private DateTime _inviteDate;
        private DateTime? _joinDate;

        // keys
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? ProjectId { get; set; }
        [Required] public string? InvitorId { get; set; }
        public string? InviteeId { get; set; }

        // properties
        public DateTime InviteDate
        {
            get => _inviteDate.ToLocalTime();
            set => _inviteDate = value.ToUniversalTime();
        }
        public DateTime? JoinDate
        {
            get => _joinDate?.ToLocalTime();
            set => _joinDate = value.HasValue ? value.Value.ToUniversalTime() : null;
        }
        public Guid CompanyToken { get; set; }
        [Required] public string? InviteeEmail { get; set; }
        [Required] public string? InviteeFirstName { get; set; }
        [Required] public string? InviteeLastName { get; set; }
        public string? Message { get; set; }
        public bool IsValid { get; set; }

        // nav properties
        public virtual Company? Company { get; set; }
        public virtual Project? Project { get; set; }
        public virtual BTUser? Invitor { get; set; }
        public virtual BTUser? Invitee { get; set; }
    }
}