﻿using System;

namespace sdORM.Exceptions
{
    public class NoDBEntityMappingException : Exception
    {
        public NoDBEntityMappingException(Type type)
            : base($"'{type.FullName}' is not decleared as a DBEntity. Try adding the [DBEntity] attribute.")
        {

        }
    }
}