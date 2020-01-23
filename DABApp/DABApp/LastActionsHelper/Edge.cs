﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp.LastActionsHelper
{
    public class Edge
    {
        public string id { get; set; }
        public int episodeId { get; set; }
        public int userId { get; set; }
        public bool? favorite { get; set; }
        public bool? listen { get; set; }
        public int? position { get; set; }
        public string entryDate { get; set; }
        public DateTime updatedAt { get; set; }
        public DateTime createdAt { get; set; }

        //Calculate whether item has a journal or not from the entry date
        public bool? hasJournal
        {
            get
            {
                if (entryDate == null)
                {
                    return false;
                } else
                {
                    return true;
                }
            }
        }
    }
}