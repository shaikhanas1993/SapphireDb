﻿using System;

namespace SapphireDb.Sync.Models
{
    public class SyncRequest
    {
        public bool Propagate { get; set; }

        public Guid OriginId { get; set; }
    }
}