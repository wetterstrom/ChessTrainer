using ChessTrainer.Core.Models;

namespace ChessTrainer.Services;

public class GameHistoryManager
{
    public const string DefaultStartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private readonly List<BoardSnapshot> _timeline = [];
    public int CurrentIndex { get; private set; } = -1;

    public BoardSnapshot Current =>
        CurrentIndex >= 0 && CurrentIndex < _timeline.Count
            ? _timeline[CurrentIndex]
            : CreateSnapshot(DefaultStartFen, null, null);

    public IReadOnlyList<BoardSnapshot> Timeline => _timeline;
    public bool CanStepBackward => CurrentIndex > 0;
    public bool CanStepForward => CurrentIndex < _timeline.Count - 1;

    public GameHistoryManager()
    {
        Reset(DefaultStartFen);
    }

    public void Reset(string startFen = DefaultStartFen)
    {
        _timeline.Clear();
        _timeline.Add(CreateSnapshot(startFen, null, null));
        CurrentIndex = 0;
    }

    public void RecordMove(string newFen, string moveUci, string moveSan)
    {
        // Om vi har backat i historiken och gör ett nytt drag kapar vi framtida grenar
        if (CanStepForward)
        {
            _timeline.RemoveRange(CurrentIndex + 1, _timeline.Count - (CurrentIndex + 1));
        }

        _timeline.Add(CreateSnapshot(newFen, moveUci, moveSan));
        CurrentIndex = _timeline.Count - 1;
    }

    // Ångrar ett helt dragpar (ditt drag + botens svar) när du spelar mot Maia
    public BoardSnapshot TakebackMovePair()
    {
        int stepsBack = CurrentIndex >= 2 ? 2 : (CurrentIndex == 1 ? 1 : 0);
        if (stepsBack > 0)
        {
            _timeline.RemoveRange(_timeline.Count - stepsBack, stepsBack);
            CurrentIndex = _timeline.Count - 1;
        }
        return Current;
    }

    // Navigera bakåt/framåt utan att radera historiken (för "Mina Partier"-läget)
    public BoardSnapshot StepBackward()
    {
        if (CanStepBackward) CurrentIndex--;
        return Current;
    }

    public BoardSnapshot StepForward()
    {
        if (CanStepForward) CurrentIndex++;
        return Current;
    }

    public BoardSnapshot JumpToMove(int index)
    {
        if (index >= 0 && index < _timeline.Count)
            CurrentIndex = index;
        return Current;
    }

    public void AttachEvaluationToCurrent(EvaluationResult eval)
    {
        if (CurrentIndex >= 0 && CurrentIndex < _timeline.Count)
        {
            _timeline[CurrentIndex] = _timeline[CurrentIndex] with { Evaluation = eval };
        }
    }

    public void UpdateSnapshotAt(int index, EvaluationResult eval, MoveQuality quality, double winDelta)
    {
        if (index >= 0 && index < _timeline.Count)
        {
            _timeline[index] = _timeline[index] with
            {
                Evaluation = eval,
                Quality = quality,
                WinChanceDelta = winDelta
            };
        }
    }

    private static BoardSnapshot CreateSnapshot(string fen, string? uci, string? san) =>
        new(fen, uci, san, MaterialCalculator.CalculateFromFen(fen));
}