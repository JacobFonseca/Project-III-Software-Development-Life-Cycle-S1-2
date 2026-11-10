using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroFlow.Core;

/// <summary>Anything that can give the next simulated value (e.g. a looping file reader).</summary>
public interface IDataSource
{
    double ReadNext();
}