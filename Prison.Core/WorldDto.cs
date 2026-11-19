using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Prison.Core
{
    [XmlRoot("PrisonWorld")]
    public class WorldDto
    {
        [XmlAttribute("SavedAt")]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        [XmlArray("Blocks")]
        [XmlArrayItem("Block")]
        public List<PrisonBlockDto> Blocks { get; set; } = new();
    }

    public class PrisonBlockDto
    {
        [XmlAttribute("BlockName")]
        public string BlockName { get; set; } = string.Empty;

        [XmlArray("Inmates")]
        [XmlArrayItem("Prisoner")]
        public List<PrisonerDto> Inmates { get; set; } = new();
    }

    public class PrisonerDto
    {
        public Guid Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Energy { get; set; }
    }
}