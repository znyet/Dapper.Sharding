﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Dapper.Sharding
{
    internal partial class SqlServerTable<T> : ITable<T> where T : class
    {
        public override bool DeleteAsync(object id)
        {
            throw new NotImplementedException();
        }
    }
}
