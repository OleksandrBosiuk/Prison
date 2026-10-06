# Prison Block 13 — WPF OOP Application

## Theme & Goal
The application simulates a prison environment ("Prison Block 13") where different inmate types coexist and react polymorphically to actions. The solution is partitioned into a core domain library (`Prison.Core`) and a user interface (`Prison.WpfApp`), showcasing inheritance, interfaces, polymorphism, state validation, and UI data binding.

## Architecture & Interfaces
* **`Prisoner` (abstract base class):**
  * Holds identity (`Name`, assigned in constructor, immutable) and protected state (`Energy`, range 0–100 with strict validation in setter).
  * Implements `INotifyPropertyChanged` to dynamically notify the UI when state changes.
  * Declares virtual/abstract behaviors: `NormalAction()`, `Rest()`, and `CrazyAction()`.
* **Interfaces:**
  * `IWork` — declares `string Work()`
  * `IStudy` — declares `string Study()`
  * `IPlayChess` — declares `string PlayChess()` (added via peer review)
* **Derived Subclasses:**
  * `Rookie` — inherits `Prisoner`, implements `IWork`.
  * `Philosopher` — inherits `Prisoner`, implements `IStudy`.
  * `Artist` — inherits `Prisoner`, implements both `IWork` and `IStudy`.
  * `ChessMaster` — inherits `Prisoner`, implements `IStudy` and `IPlayChess`.

## CrazyAction & State Validation
* **Validation Rules:**
  * Inmate energy is strictly clamped between 0 and 100 via `Math.Max(0, Math.Min(100, value))`. Invalid actions or state changes cannot corrupt valid state.
  * Empty input strings throw an `ArgumentException` at domain level, caught by the UI via `try-catch` to avoid application termination.
* **CrazyAction Behaviors:**
  * `Rookie`: Panics and tries to escape through vents (-50 energy).
  * `Philosopher`: Holds an intense philosophical lecture to guards (-25 energy).
  * `Artist`: Checks state; fails if energy < 30. Otherwise spends 30 energy painting a portrait with toothpaste.
  * `ChessMaster`: Flips the table accusing the bishop of sabotage (-35 energy).

## Independently Learned WPF Element
* **Element:** `ProgressBar`
* **Justification:** Chosen to provide immediate visual feedback for the inmate's validated `Energy` property (0–100). Bound via OneWay DataBinding and updates smoothly in real time thanks to `INotifyPropertyChanged`.

## Verified Use Cases
1. **Adding an Inmate:** Select class, enter valid name, click "Add Prisoner" — inmate appears in `ObservableCollection` and log.
2. **Input Validation:** Submit empty or whitespace name — domain throws exception, UI catches it and displays a warning dialog without crashing.
3. **Type-Safe Capability Check (`is` operator):** Invoke "Work" on `Philosopher` — UI verifies capability via `is IWork`, gracefully reporting inability instead of failing.
4. **Energy Bounds:** Repeatedly click "Rest" — energy level increases by 40 per click but never exceeds the maximum bound of 100.
5. **Item Removal:** Select an inmate and click "Remove Selected" — instance is safely removed from the collection and UI roster.

## Git Collaboration
* **Collaborator:** [Peer Name]
* **Issue:** [Link to Issue]
* **Pull Request:** [Link to Pull Request]

## AI Usage Disclosure
* **Tool:** Gemini
* **Purpose:** Refactoring suggestions, structuring `.resx` resource architecture, and terminal UI design tuning.
* **Verification:** All classes, inheritance hierarchies, interfaces, data-binding expressions, and error handling were manually verified, tested, and debugged.
