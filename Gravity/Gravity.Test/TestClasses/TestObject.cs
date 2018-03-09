using Gravity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Test.TestClasses
{
    [Serializable]
    [RelativityObject("36A1E2D5-138A-4FAB-9EE4-13C686EB56B4")]
    class TestObject : BaseDto
    {
        [RelativityObjectField("F2BDFD76-7E77-4D4A-8FBE-07894BFAB43B", (int)RdoFieldType.FixedLengthText, 255)]
        public override string Name { get; set; }

        [RelativityObjectField("A8B5B93B-2980-498E-8758-F332987612AC", (int)RdoFieldType.FixedLengthText, 100)]
        public string TextField { get; set; }

        [RelativityObjectField("BBBE0524-C1B5-4E79-8C46-42D9CD0AE05C", (int)RdoFieldType.WholeNumber)]
        public string WholeNumber { get; set; }
        

    }
}
