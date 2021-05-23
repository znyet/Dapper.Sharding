﻿using Dapper.Sharding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test2
{
    [Table("Id", false, "老师表", "Log")]
    public class Teacher
    {
        [Column(20, "主键Id")]
        public long Id { get; set; }

        [Column(0, "名字")]
        public string Name { get; set; }

        public int Age { get; set; }
    }
}