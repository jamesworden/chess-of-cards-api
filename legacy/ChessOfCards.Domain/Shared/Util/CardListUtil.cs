using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.Domain.Shared.Util;

public static class CardListUtil
{
  public static bool ContainsDifferentCards(this List<Card> list1, List<Card> list2)
  {
    if (list1.Count != list2.Count)
    {
      return true;
    }

    for (var i = 0; i < list1.Count; i++)
    {
      var card1 = list1[i];
      var hasCard = false;

      for (var j = 0; j < list2.Count; j++)
      {
        var card2 = list2[j];

        var kindMatches = card1.Kind == card2.Kind;
        var suitMatches = card1.Suit == card2.Suit;

        if (kindMatches && suitMatches)
        {
          hasCard = true;
          break;
        }
      }

      if (!hasCard)
      {
        return true;
      }
    }
    return false;
  }
}
