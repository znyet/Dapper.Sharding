﻿using Dapper.Sharding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Com
{
    [Table("Id", true, "老师表")]
    public class Teacher
    {
        [Column("主键Id")]
        public long Id { get; set; }

        [Column("名字", 4)]
        public string Name { get; set; }

        public byte Sex { get; set; }

        public short Age { get; set; }
    }
}