using System.Collections.Generic;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
private static List<string> GetExclusions(string exclusionString)
        {
            if (string.IsNullOrEmpty(exclusionString))
            {
                return new List<string>();
            }

            var exclusionList = new List<string>();
            string[] exclusionsArray = exclusionString.Split(',');

            foreach (string exclusion in exclusionsArray)
            {
                if (exclusion.Contains(','))
                {
                    exclusionList.AddRange(GetExclusions(exclusion));
                }
                else
                {
                    exclusionList.Add(exclusion.Trim());
                }
            }

            return exclusionList;
        }
}
