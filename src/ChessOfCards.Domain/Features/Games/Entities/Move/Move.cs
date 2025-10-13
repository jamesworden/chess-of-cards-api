namespace ChessOfCards.Domain.Features.Games;

public class Move(List<PlaceCardAttempt> PlaceCardAttempts)
{
    public List<PlaceCardAttempt> PlaceCardAttempts { get; set; } = PlaceCardAttempts;

    public bool MovesMatch(Move move)
    {
        if (PlaceCardAttempts.Count != move.PlaceCardAttempts.Count)
        {
            return false;
        }

        for (var i = 0; i < PlaceCardAttempts.Count; i++)
        {
            var attempt1 = PlaceCardAttempts[i];
            var attempt2 = move.PlaceCardAttempts[i];

            if (attempt1.TargetLaneIndex != attempt2.TargetLaneIndex)
            {
                return false;
            }
            else if (attempt1.TargetRowIndex != attempt2.TargetRowIndex)
            {
                return false;
            }
            else if (!attempt1.Card.SuitAndKindMatch(attempt2.Card))
            {
                return false;
            }
        }

        return true;
    }

    public bool ContainsConsecutivePlaceCardAttempts()
    {
        var targetRowIndexes = PlaceCardAttempts
            .Select(placeCardAttempt => placeCardAttempt.TargetRowIndex)
            .ToList();
        targetRowIndexes.Sort();

        for (int i = 0; i < targetRowIndexes.Count - 1; i++)
        {
            var upperIndex = targetRowIndexes[i + 1];
            var lowerIndex = targetRowIndexes[i];
            var indexesSurroundMiddle = upperIndex > 3 && lowerIndex < 3;
            var rowIndexesSeperate = upperIndex - lowerIndex != 1;
            if (rowIndexesSeperate && !indexesSurroundMiddle)
            {
                return false;
            }
        }

        return true;
    }

    public PlaceCardAttempt? GetInitialPlaceCardAttempt(bool asHostPlayer)
    {
        var initialPlaceCardAttempt = PlaceCardAttempts.FirstOrDefault();
        if (initialPlaceCardAttempt is null)
        {
            return null;
        }

        foreach (var placeCardAttempt in PlaceCardAttempts)
        {
            var isMoreInitial = asHostPlayer
                ? placeCardAttempt.TargetRowIndex < initialPlaceCardAttempt.TargetRowIndex
                : placeCardAttempt.TargetRowIndex > initialPlaceCardAttempt.TargetRowIndex;

            if (isMoreInitial)
            {
                initialPlaceCardAttempt = placeCardAttempt;
            }
        }

        return initialPlaceCardAttempt;
    }
}
