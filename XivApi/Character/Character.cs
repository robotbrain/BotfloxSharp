﻿using System.Collections.Generic;

namespace XivApi.Character
{
    public class Character
    {
        public string Avatar { get; set; }
        public List<ClassJob> ClassJobs { get; set; }
        public ClassJobsElemental ClassJobsElemental { get; set; }
        public string DC { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Nameday { get; set; }
        public string Portrait { get; set; }
        public Race Race { get; set; }
        public string Server { get; set; }
        public Title Title { get; set; }
        public bool TitleTop { get; set; }
        public Tribe Tribe { get; set; }
    }
}