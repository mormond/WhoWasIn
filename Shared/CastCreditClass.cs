using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace whoWasIn.Shared {
    [Serializable]
    public class CastCredit
    {
      public int cast_id;
      public string character;
      public string credit_id;
      public int id;
      public string name;
      public int order;
      public string profile_path;
    }
}