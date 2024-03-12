using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class NmaClub
    {
        public int Id { get; set; }

        public string ClubName { get; set; }

        public IEnumerable<ClubTeam>? U10 { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? U13 { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? U16 { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? U19 { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? Men { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? Veteran { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? Master { get; set; } = new List<ClubTeam>();

        public IEnumerable<ClubTeam>? Women { get; set; } = new List<ClubTeam>();
    }
}
