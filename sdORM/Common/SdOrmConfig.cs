﻿using sdORM.Mapping;

namespace sdORM.Common
{
    public class SdOrmConfig
    {
        public string ConnectionString { get; set; }

        public EntityMappingProvider Mappings { get; set; }
    }
}