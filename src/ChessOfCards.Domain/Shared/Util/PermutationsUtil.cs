namespace ChessOfCards.Domain.Shared.Util;

public class PermutationsUtil
{
    public static List<List<T>> GetSubsetsPermutations<T>(List<T> list)
    {
        var subsets = new List<List<T>>();
        GenerateSubsetsPermutations(list, 0, [], subsets);
        return subsets;
    }

    static void GenerateSubsetsPermutations<T>(
        List<T> list,
        int index,
        List<T> subset,
        List<List<T>> subsets
    )
    {
        if (index == list.Count)
        {
            if (subset.Count > 0)
            {
                subsets.Add(new List<T>(subset));
                permutationsOfSubset(subset, subsets);
            }
        }
        else
        {
            // Include current element
            subset.Add(list[index]);
            GenerateSubsetsPermutations(list, index + 1, subset, subsets);

            // Exclude current element
            subset.RemoveAt(subset.Count - 1);
            GenerateSubsetsPermutations(list, index + 1, subset, subsets);
        }
    }

    static void permutationsOfSubset<T>(List<T> subset, List<List<T>> result)
    {
        var permutations = new List<List<T>>();
        GeneratePermutations(subset, 0, subset.Count - 1, permutations);
        result.AddRange(permutations);
    }

    static void GeneratePermutations<T>(
        List<T> list,
        int startIndex,
        int endIndex,
        List<List<T>> result
    )
    {
        if (startIndex == endIndex)
        {
            result.Add(new List<T>(list));
        }
        else
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                Swap(list, startIndex, i);
                GeneratePermutations(list, startIndex + 1, endIndex, result);
                Swap(list, startIndex, i); // Backtrack
            }
        }
    }

    static void Swap<T>(List<T> list, int i, int j)
    {
        (list[j], list[i]) = (list[i], list[j]);
    }
}
