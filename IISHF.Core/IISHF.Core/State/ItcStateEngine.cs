using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Enums;
using IISHF.Core.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.State
{
    public static class ItcStateMachine
    {
        public sealed class ItcEvaluation
        {
            public ItcState State { get; }
            public DateTime? StatusChangeDate { get; }

            public ItcEvaluation(ItcState state, DateTime? statusChangeDate)
            {
                State = state;
                StatusChangeDate = statusChangeDate;
            }
        }

        public static ItcEvaluation Evaluate(IPublishedContent teamNode)
        {
            // Inputs
            var submitted = teamNode.Value<bool>("iTCSubmitted");
            var rejectionReason = teamNode.Value<string>("iTCRejectionReason");

            var nmaApprover = teamNode.Value<IPublishedContent>("iTCNMAApprover");
            var nmaApprovedDate = teamNode.Value<DateTime>("nMAApprovedDate");
            var submissionDate = teamNode.Value<DateTime>("iTCSubmissionDate");

            // Derivations
            var hasRejectionReason = !string.IsNullOrWhiteSpace(rejectionReason);
            var hasNmaApprover = nmaApprover != null;
            var hasNmaApprovedDate = nmaApprovedDate != DateTime.MinValue;
            var hasSubmissionDate = submissionDate != DateTime.MinValue;

            // State evaluation (kept close to your existing behaviour, but centralized)
            ItcState state;

            if (!submitted && !hasRejectionReason)
            {
                state = ItcState.NotSubmitted;
                return new ItcEvaluation(state, null);
            }

            // If there is a rejection reason shown, your original code flips between:
            // - "Changes required" (rejection exists)
            // - "Changes made" (submitted && rejection exists)
            if (hasRejectionReason)
            {
                state = submitted ? ItcState.ChangesMade : ItcState.ChangesRequired;

                // Your current code only sets a change date in some branches; keep it conservative.
                // If you DO store a "rejected date" property later, this is the place to hook it.
                return new ItcEvaluation(state, null);
            }

            // Submitted, no rejection reason:
            // If no NMA approver yet => Pending NMA Approval (change date = submission date when available)
            if (submitted && !hasNmaApprover)
            {
                state = ItcState.PendingNmaApproval;
                return new ItcEvaluation(state, hasSubmissionDate ? submissionDate : null);
            }

            // If NMA approver exists and NMA approved date exists => NMA Approved - Pending IISHF Approval
            if (submitted && hasNmaApprover && hasNmaApprovedDate)
            {
                state = ItcState.NmaApprovedPendingIishfApproval;
                return new ItcEvaluation(state, nmaApprovedDate);
            }

            // Fallback: if submitted + NMA approver exists but approved date missing, keep it as pending NMA approval.
            // (This matches the spirit of the current UI logic more than inventing a new state.)
            state = ItcState.PendingNmaApproval;
            return new ItcEvaluation(state, hasSubmissionDate ? submissionDate : null);
        }
    }
}
