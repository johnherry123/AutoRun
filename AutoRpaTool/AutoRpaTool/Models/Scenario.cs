using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRpaTool.Models
{
    public class Scenario
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string? Description { get; set; } 

        public List<ActionNode> Nodes { get; set; } = new List<ActionNode>();
    }
}
