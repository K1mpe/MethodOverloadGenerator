# MethodOverloadGenerator

When a method accepts a delegate, callers are often forced to write wrapper code that adds no real value:

```csharp
// Forced to wrap a plain value
await shelter.Admit(() => Task.FromResult(new Dog { Name = "Rex" }));

// Forced to accept parameters that are never used
var cat = await animalShelter.GetCatAsync("Fluffy");
await cat.Feed(async (x, y, z) => await preyService.GetNextPreyAsync());
```

This generator eliminates that boilerplate. Place `[MethodOverloadGenerator]` on a parameter, method, constructor, or class and the generator automatically produces overloads that let callers pass exactly what they have — a sync delegate instead of an async one, a plain value instead of a delegate, or a delegate with fewer parameters — without wrapping anything:

```csharp
// Clean — no Task.FromResult, no unused parameters
await shelter.Admit(new Dog { Name = "Rex" });
await animalShelter
    .GetCatAsync("Fluffy")
    .Feed(preyService.GetNextPreyAsync);
```

---

## Examples

### Method / parameter level — service class

```csharp
public partial class AnimalShelter
{
    [MethodOverloadGenerator]
    public AnimalShelter(
        Func<Task<int>> getAvailableCapacity,
        Func<Task<TimeSpan>> fetchNextFeedingTime) { ... }

    [MethodOverloadGenerator]
    public Task<bool> Admit(Func<Task<Dog>> fetchDog) { ... }

    [MethodOverloadGenerator]
    public Task ScheduleFeeding(Func<Task<TimeSpan>> getInterval, Func<List<ICarnivore>, Task> feedAll) { ... }
}
```

**Usage:**
```csharp
// Fully async — delegates from real services
var shelter = new AnimalShelter(
    getAvailableCapacity: db.GetCapacityAsync,
    fetchNextFeedingTime: schedule.GetNextFeedingAsync);

// Mixed — fixed capacity, async feeding schedule
var shelter = new AnimalShelter(
    getAvailableCapacity: 20,
    fetchNextFeedingTime: schedule.GetNextFeedingAsync);

// Fully fixed — ideal for tests
var shelter = new AnimalShelter(
    getAvailableCapacity: 20,
    fetchNextFeedingTime: TimeSpan.FromHours(4));

// Admit — async fetch, sync factory, or a ready instance
await shelter.Admit(rescue.FetchNextDogAsync);
await shelter.Admit(() => new Dog { Name = "Rex" });
await shelter.Admit(new Dog { Name = "Rex" });

// ScheduleFeeding — mix async interval with a sync feeding action
await shelter.ScheduleFeeding(
    getInterval: TimeSpan.FromHours(4),
    feedAll:     animals => feeder.FeedAll(animals));
```

---
### Class-level attribute — static extension methods

```csharp
public delegate Task<IPrey> HuntAsync();
public delegate Task RelocateAsync(Location destination);

[MethodOverloadGenerator]
public static partial class AnimalExtensions
{
    // Func<> — feeds any carnivore using an async or sync prey source
    public static Task Feed(this ICarnivore animal, Func<Task<IPrey>> getPrey) { ... }

    // Named delegate — teaches a dog to hunt using a hunt strategy
    public static Task<bool> Teach(this Dog dog, HuntAsync hunt) { ... }

    // Named delegate — migrates a bird to a destination
    public static Task Migrate(this IBird bird, RelocateAsync relocate) { ... }

    // Skipped automatically — out parameter cannot be made async
    public static bool TryGetHunger(this ICarnivore animal, out int hungerLevel) { ... }
}
```

**Usage:**
```csharp
var cat = new Cat();
var dog = new Dog();
var hawk = new Hawk(); // implements both ICarnivore and IBird

// Feed — async, sync, or fixed prey
await cat.Feed(preyService.GetNextPreyAsync);
await cat.Feed(() => new Mouse());
await cat.Feed(new Mouse());

// Feed — chain directly off an async result (Rule 4), no intermediate await
await GetCatAsync().Feed(preyService.GetNextPreyAsync);
await GetCatAsync().Feed(new Mouse());

// Teach — async hunt strategy, sync, or fixed result
await dog.Teach(wildernessService.HuntAsync);
await dog.Teach(() => wilderness.Hunt());

// Migrate — async or sync relocation
await hawk.Migrate(async dest => await flightService.FlyToAsync(dest));
await hawk.Migrate(dest => map.MoveTo(dest));
```

---



## Installation

> Package not yet published. Reference the project directly for now.

---

## Usage

The attribute can be placed at three levels and works on both methods and constructors. The containing class must always be `partial`.

### Parameter level

Affects only the decorated delegate parameter.

```csharp
public partial class MyService
{
    public async Task MyMethod([MethodOverloadGenerator] Func<Task<int>> func)
    {
        var result = await func();
        Console.WriteLine(result);
    }

    public MyService([MethodOverloadGenerator] Func<Task<int>> func)
    {
        ...
    }
}
```

### Method / constructor level

Equivalent to placing `[MethodOverloadGenerator]` on every delegate parameter of that method or constructor. All combinations of overloads are generated (see [Multiple attributed parameters](#multiple-attributed-parameters--combinatorial-overloads)).

```csharp
public partial class MyService
{
    [MethodOverloadGenerator]
    public async Task MyMethod(Func<Task<int>> func1, Func<Task<string>> func2)
    {
        var a = await func1();
        var b = await func2();
    }

    [MethodOverloadGenerator]
    public MyService(Func<Task<int>> func1, Func<Task<string>> func2)
    {
        ...
    }
}
```

### Class level

Equivalent to placing `[MethodOverloadGenerator]` on every method and constructor in the class.

```csharp
[MethodOverloadGenerator]
public partial class MyService
{
    public MyService(Func<Task<int>> func) { ... }
    public async Task MethodA(Func<Task<int>> func) { ... }
    public async Task MethodB(Func<Task<string>> func) { ... }
}
```

---

The generator emits the overloads as a separate `partial` file — you never write them by hand.

---

## Generated overloads

### Rule 1 — Async delegate → sync overload

When the delegate returns `Task<T>` or `ValueTask<T>`, the generator adds a sync `Func<T>` overload.
When the delegate returns `Task` or `ValueTask` (no return value), the generator adds an `Action` overload.

**`Func<Task<int>>` → `Func<int>`:**
```csharp
public async Task MyMethod([MethodOverloadGenerator] Func<Task<int>> func)
{
    var result = await func();
    Console.WriteLine(result);
}

// Generated
public async Task MyMethod(Func<int> func) => MyMethod(() => Task.FromResult(func()));
```

**`Func<ValueTask<int>>` → `Func<int>`:**
```csharp
public async Task MyMethod([MethodOverloadGenerator] Func<ValueTask<int>> func)
{
    var result = await func();
    Console.WriteLine(result);
}

// Generated
public async Task MyMethod(Func<int> func) => MyMethod(() => new ValueTask<int>(func()));
```

**`Func<int, Task>` → `Action<int>`:**
```csharp
public async Task MyMethod([MethodOverloadGenerator] Func<int, Task> func)
{
    await func(42);
}

// Generated
public async Task MyMethod(Action<int> action) => MyMethod(x => { action(x); return Task.CompletedTask; });
```

**Callers can now use either:**
```csharp
await MyMethod(async x => await DoWorkAsync(x));   // original async delegate
await MyMethod(x => DoWork(x));                    // sync Action<int>
```

---

### Rule 2 — Value-returning delegate → fixed-value overload

When the delegate returns a value (whether async or not), the generator adds a plain-value overload.

**Async delegate (`Func<Task<int>>`):**
```csharp
// Generated alongside the sync overload from Rule 1
public async Task MyMethod(int value) => MyMethod(() => Task.FromResult(value));
```

**Async delegate (`Func<ValueTask<int>>`):**
```csharp
// Generated alongside the sync overload from Rule 1
public async Task MyMethod(int value) => MyMethod(() => new ValueTask<int>(value));
```

**Sync delegate (`Func<int>`):**
```csharp
public Task MyMethod([MethodOverloadGenerator] Func<int> func) { ... }

// Generated
public Task MyMethod(int value) => MyMethod(() => value);
```

**Callers can now use any of these (async case):**
```csharp
await MyMethod(async () => await GetValueAsync());   // original async delegate
await MyMethod(() => ComputeValue());                // sync delegate (Rule 1)
await MyMethod(42);                                  // fixed value  (Rule 2)
```

---

### Rule 3 — Multi-parameter delegate → trailing-parameter overloads

When the delegate has multiple input parameters, the generator creates additional overloads that progressively drop trailing parameters.

**You write:**
```csharp
public async Task MyMethod([MethodOverloadGenerator] Func<int, double, bool, Task<int>> func)
{
    var result = await func(1, 2.0, true);
    Console.WriteLine(result);
}
```

**Generated:**
```csharp
// Async overloads — fewer and fewer parameters
public async Task MyMethod(Func<int, double, Task<int>> func) => MyMethod((a, b, c) => func(a, b));
public async Task MyMethod(Func<int, Task<int>> func)         => MyMethod((a, b, c) => func(a));
public async Task MyMethod(Func<Task<int>> func)              => MyMethod((a, b, c) => func());

// Sync overloads — all parameter counts
public async Task MyMethod(Func<int, double, bool, int> func) => MyMethod((a, b, c) => Task.FromResult(func(a, b, c)));
public async Task MyMethod(Func<int, double, int> func)       => MyMethod((a, b, c) => Task.FromResult(func(a, b)));
public async Task MyMethod(Func<int, int> func)               => MyMethod((a, b, c) => Task.FromResult(func(a)));
public async Task MyMethod(Func<int> func)                    => MyMethod((a, b, c) => Task.FromResult(func()));

// Fixed value
public async Task MyMethod(int value) => MyMethod((a, b, c) => Task.FromResult(value));
```

**Callers can now use any of these:**
```csharp
// Original — all three parameters, async
await MyMethod(async (x, y, flag) => await ComputeAsync(x, y, flag));

// Drop trailing parameters — async
await MyMethod(async (x, y) => await ComputeAsync(x, y));
await MyMethod(async x     => await ComputeAsync(x));
await MyMethod(async ()    => await ComputeAsync());

// Drop trailing parameters — sync
await MyMethod((x, y, flag) => Compute(x, y, flag));
await MyMethod((x, y)       => Compute(x, y));
await MyMethod(x            => Compute(x));
await MyMethod(()           => Compute());

// Fixed value — ignore all parameters entirely
await MyMethod(42);
```

---

### Rule 4 — Extension method → overload for `Task<T>` / `ValueTask<T>`

For any extension method, the generator adds overloads where the `this` parameter is wrapped in `Task<T>` and `ValueTask<T>`. This applies to both async and non-async extension methods. The generated overloads are always async (they must await the input), but the method name never changes — no `Async` suffix is added.

**Async extension method (`Task<TOut>` return):**
```csharp
public static Task<TOut> MyExtension<TIn, TOut>(this TIn input) { ... }

// Generated
public static async Task<TOut> MyExtension<TIn, TOut>(this Task<TIn> input)
    => await (await input).MyExtension<TIn, TOut>();

public static async Task<TOut> MyExtension<TIn, TOut>(this ValueTask<TIn> input)
    => await (await input).MyExtension<TIn, TOut>();
```

**Non-async extension method (`TOut` return) — overload becomes async:**
```csharp
public static TOut MyExtension<TIn, TOut>(this TIn input) { ... }

// Generated
public static async Task<TOut> MyExtension<TIn, TOut>(this Task<TIn> input)
    => (await input).MyExtension<TIn, TOut>();

public static async Task<TOut> MyExtension<TIn, TOut>(this ValueTask<TIn> input)
    => (await input).MyExtension<TIn, TOut>();
```

**Callers can chain directly off an async source in both cases:**
```csharp
// Without the overload — manual await required
var result = await (await GetInputAsync()).MyExtension<MyType, string>();

// With the overload — chain directly
var result = await GetInputAsync().MyExtension<MyType, string>();
```

---

### Rule 5 - Multiple attributed parameters — combinatorial overloads

When more than one parameter carries `[MethodOverloadGenerator]`, every combination of their individual overloads is generated.

**You write:**
```csharp
public async Task MyMethod(
    [MethodOverloadGenerator] Func<Task<int>> func1,
    [MethodOverloadGenerator] Func<Task<string>> func2)
{
    var a = await func1();
    var b = await func2();
}
```

**Generated (all combinations):**
```csharp
// func1 sync, func2 original
public async Task MyMethod(Func<int> func1,    Func<Task<string>> func2) => MyMethod(() => Task.FromResult(func1()), func2);

// func1 value, func2 original
public async Task MyMethod(int value1,         Func<Task<string>> func2) => MyMethod(() => Task.FromResult(value1), func2);

// func1 original, func2 sync
public async Task MyMethod(Func<Task<int>> func1, Func<string> func2)    => MyMethod(func1, () => Task.FromResult(func2()));

// func1 original, func2 value
public async Task MyMethod(Func<Task<int>> func1, string value2)         => MyMethod(func1, () => Task.FromResult(value2));

// func1 sync, func2 sync
public async Task MyMethod(Func<int> func1,    Func<string> func2)       => MyMethod(() => Task.FromResult(func1()), () => Task.FromResult(func2()));

// func1 sync, func2 value
public async Task MyMethod(Func<int> func1,    string value2)            => MyMethod(() => Task.FromResult(func1()), () => Task.FromResult(value2));

// func1 value, func2 sync
public async Task MyMethod(int value1,         Func<string> func2)       => MyMethod(() => Task.FromResult(value1), () => Task.FromResult(func2()));

// func1 value, func2 value
public async Task MyMethod(int value1,         string value2)            => MyMethod(() => Task.FromResult(value1), () => Task.FromResult(value2));
```

The number of generated overloads grows multiplicatively: if the first parameter has 3 variants and the second has 3 variants, 8 combinations are emitted (3 × 3, minus the original). In practice this is rarely a concern — the source generator only runs when the source file changes, and a method with so many attributed delegate parameters would be a design smell regardless.

---

## Rules summary

| Condition | Overloads generated |
|---|---|
| Delegate returns `Task<T>` or `ValueTask<T>` | Sync `Func<…, T>` overload |
| Delegate returns `Task` or `ValueTask` | Sync `Action<…>` overload |
| Delegate returns any value (`Task<T>`, `ValueTask<T>`, or `T`) | Fixed-value (`T`) overload |
| Delegate has multiple parameters | Overloads that omit trailing parameters one by one |
| Extension method | Overloads for `this Task<T>` and `this ValueTask<T>` |

All rules compose: an async multi-parameter delegate gets the full matrix of sync + parameter-stripped overloads.

---

## IntelliSense / overload-resolution priority

Every generated overload carries [`[OverloadResolutionPriority]`](https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-13.0/overload-resolution-priority) (a .NET 9+ attribute; the generator polyfills it for older target frameworks, so it works regardless of what the consuming project targets). This is what both the C# compiler and Visual Studio's IntelliSense use to rank overloads, so:

- **The original method is always suggested first.** It's never touched by the generator, so it keeps the implicit priority of `0` — the highest of any overload.
- **Among the generated overloads, the ones that keep the most of the original delegate's parameters — and keep it async — rank next**, and the ones that give up the most, or additionally turn an async delegate synchronous, rank last.

Concretely, every generated overload's priority is `-(1 + reduction)`, where `reduction` is how many delegate input parameters that overload gives up relative to the original, plus one extra point whenever it also turns an originally-async delegate synchronous, summed across every substituted parameter:

| Substitution | Reduction | Example (`Func<int, string, Task<bool>>`, N = 2 inputs) |
|---|---|---|
| Rule 4 — `Task<T>`/`ValueTask<T>` receiver only | `0` | priority `-1` (no delegate arity or async-ness changes) |
| Rule 3, async-drop — arity `N` → `k`, stays async | `N - k` | `Func<int, Task<bool>>` (k=1) → priority `-2` |
| Rule 1 — sync, same arity (always loses async) | `1` | `Func<int, string, bool>` → priority `-2` |
| Rule 3, fully-sync — arity `N` → `k`, also loses async | `(N - k) + 1` | `Func<int, bool>` (k=1) → priority `-3` |
| Rule 2 — fixed value (never async) | `N + 1` (+`1` more if the delegate was async) | `bool value` → priority `-5` |

So when the original delegate is async, an overload that keeps it async always outranks an otherwise-equivalent overload that turns it synchronous — e.g. Rule 4's receiver-only overloads (which never touch the delegate's async-ness) rank *above* Rule 1's sync overloads. Rule 5's combinatorial overloads sum the reduction across every parameter they substitute, so an overload that changes two parameters ranks below one that changes only one of them the same way.

This ordering is a heuristic, not a strict specification — ties are expected and fine (e.g. Rule 3's async-drop form at one `k` can tie with Rule 1 at a different `k`). The only hard guarantee is that the original method always wins, and an async-preserving overload always outranks an otherwise-equivalent sync one.

---

## Controlling which rules apply

Each rule can be individually enabled or disabled. There are two places to configure this, and they compose:

| Priority | Where | Scope |
|---|---|---|
| 1 (highest) | Attribute parameter | The single decorated parameter, method, constructor, or class |
| 2 | MSBuild property in `.csproj` | Every use of the generator in the project |
| 3 (lowest) | Built-in default | **Enabled** — all rules apply when nothing is specified |

A rule runs unless it has been explicitly set to `RuleOverride.Disable` somewhere in this chain. Leaving a parameter at its default (`RuleOverride.Default`, or omitting it entirely) means "defer to the next level down".

> **Why `RuleOverride` and not `bool?`** — C# attribute arguments are restricted to a fixed set of types (`bool`, `int`, `string`, `enum`, `Type`, `object`, and 1-D arrays of those). `bool?` (`Nullable<bool>`) is not on that list, so a three-value enum is used instead.

### Attribute parameters

`[MethodOverloadGenerator]` accepts one optional `RuleOverride` per rule. The `RuleOverride` enum has three values: `Default` (inherit), `Enable` (force on), and `Disable` (force off).

```csharp
[MethodOverloadGenerator(
    syncOverloads:              RuleOverride.Default,  // Rule 1 — Default = use project default
    valueOverloads:             RuleOverride.Default,  // Rule 2 — Default = use project default
    trailingParameterOverloads: RuleOverride.Default,  // Rule 3 — Default = use project default
    taskReceiverOverloads:      RuleOverride.Default,  // Rule 4 — Default = use project default
    combinatorialOverloads:     RuleOverride.Default)] // Rule 5 — Default = use project default
```

All parameters default to `RuleOverride.Default`, so `[MethodOverloadGenerator]` with no arguments behaves exactly as before. Only set a parameter when you need to override the project default for that specific element.

| Attribute parameter | Controls |
|---|---|
| `syncOverloads` | Rule 1 — async delegate → sync `Func<T>` / `Action` overload |
| `valueOverloads` | Rule 2 — value-returning delegate → fixed-value overload |
| `trailingParameterOverloads` | Rule 3 — multi-parameter delegate → trailing-parameter overloads |
| `taskReceiverOverloads` | Rule 4 — extension method → `Task<T>` / `ValueTask<T>` receiver overloads |
| `combinatorialOverloads` | Rule 5 — multiple attributed parameters → combinatorial overloads |

### MSBuild properties

Set project-wide defaults in the `.csproj`. Any rule not listed here falls back to the built-in default (enabled).

```xml
<PropertyGroup>
    <MethodOverloadGenerator_SyncOverloads>true</MethodOverloadGenerator_SyncOverloads>
    <MethodOverloadGenerator_ValueOverloads>true</MethodOverloadGenerator_ValueOverloads>
    <MethodOverloadGenerator_TrailingParameterOverloads>true</MethodOverloadGenerator_TrailingParameterOverloads>
    <MethodOverloadGenerator_TaskReceiverOverloads>true</MethodOverloadGenerator_TaskReceiverOverloads>
    <MethodOverloadGenerator_CombinatorialOverloads>true</MethodOverloadGenerator_CombinatorialOverloads>
</PropertyGroup>
```

| MSBuild property | Controls |
|---|---|
| `MethodOverloadGenerator_SyncOverloads` | Rule 1 |
| `MethodOverloadGenerator_ValueOverloads` | Rule 2 |
| `MethodOverloadGenerator_TrailingParameterOverloads` | Rule 3 |
| `MethodOverloadGenerator_TaskReceiverOverloads` | Rule 4 |
| `MethodOverloadGenerator_CombinatorialOverloads` | Rule 5 |

### Examples

**Disable Rule 3 project-wide, re-enable it on one method:**

```csharp
// .csproj
// <MethodOverloadGenerator_TrailingParameterOverloads>false</MethodOverloadGenerator_TrailingParameterOverloads>

public partial class AnimalShelter
{
    // Rule 3 off project-wide — only Rule 1 and Rule 2 overloads are generated here
    [MethodOverloadGenerator]
    public Task<bool> Admit(Func<Task<Dog>> fetchDog) { ... }

    // Explicitly opt Rule 3 back in for this method only
    [MethodOverloadGenerator(trailingParameterOverloads: RuleOverride.Enable)]
    public Task ScheduleFeeding(Func<int, double, Task<TimeSpan>> getInterval) { ... }
}
```

**Keep only value overloads (Rule 2) for a single parameter:**

```csharp
public partial class AnimalShelter
{
    public Task<bool> Admit(
        [MethodOverloadGenerator(syncOverloads: RuleOverride.Disable, trailingParameterOverloads: RuleOverride.Disable)]
        Func<Task<Dog>> fetchDog) { ... }

    // Generated: only the fixed-value overload — Admit(Dog dog)
    // The sync Func<Dog> overload is suppressed.
}
```

**Disable combinatorial overloads globally to cap generated code size:**

```xml
<!-- .csproj — combinatorial growth can be large; disable it project-wide -->
<MethodOverloadGenerator_CombinatorialOverloads>false</MethodOverloadGenerator_CombinatorialOverloads>
```

---

## Diagnostics

Some scenarios produce a compile error; others are silently skipped at class level. The general principle: an explicit opt-in (`[MethodOverloadGenerator]` on a method or parameter) always produces an error when unsupported, while class-level opt-in skips unsupported methods silently so the rest of the class still benefits.

| Scenario | Class level | Method / parameter level |
|---|---|---|
| Method has `out` parameter | Silently skipped | Compile error |
| Method has `ref` parameter | Silently skipped | Compile error |
| Attribute on a non-delegate parameter | Silently skipped | Compile error |
| No overloads would be generated | Silently skipped | Warning on the attribute |
| Class is not `partial` | Compile error | Compile error |

### `out` and `ref` parameters

C# does not allow `out` or `ref` parameters on async methods, so no async overloads can be generated.

```csharp
// Error — explicit opt-in on a method with out parameter
[MethodOverloadGenerator]
public static bool TryGetHunger(this ICarnivore animal, out int hungerLevel) { ... }

// Error — explicit opt-in on a method with ref parameter
[MethodOverloadGenerator]
public static void Sedate(this Dog dog, ref SedationLevel level) { ... }
```

```csharp
// Silently skipped at class level — other methods still get overloads
[MethodOverloadGenerator]
public static partial class AnimalExtensions
{
    public static bool TryGetHunger(this ICarnivore animal, out int hungerLevel) { ... } // skipped
    public static void Sedate(this Dog dog, ref SedationLevel level) { ... }             // skipped
    public static Task Feed(this ICarnivore animal, Func<Task<IPrey>> getPrey) { ... }   // overloads generated
}
```

### Attribute on a non-delegate parameter

`[MethodOverloadGenerator]` has no meaning on a parameter that is not a delegate — there are no overloads to generate.

```csharp
// Error — int is not a delegate
public Task Admit([MethodOverloadGenerator] int capacity) { ... }
```

### No overloads generated

When `[MethodOverloadGenerator]` is on a method or constructor but none of its parameters match any rule, the attribute has no effect. At class level this is silently skipped; at method level a warning is reported directly on the attribute so it doesn't go unnoticed.

The most common cause is a delegate that returns `void` and has no parameters — none of the four rules apply to it.

```csharp
public partial class AnimalShelter
{
    // Warning: [MethodOverloadGenerator] produces no overloads for this method.
    // 'Action' has no return value and no parameters — no rules apply.
    [MethodOverloadGenerator]
    public Task OnFeedingComplete(Action callback) { ... }
}
```

To resolve the warning either remove the attribute or change the delegate to one the rules can act on (e.g. `Func<Task>` if you want a sync `Action` overload).

### Non-partial class

The generator emits overloads into a separate `partial` file. If the class is not declared `partial`, this file cannot be merged and a compile error is emitted regardless of where the attribute is placed.

```csharp
// Error — class must be partial
[MethodOverloadGenerator]
public static class AnimalExtensions
{
    public static Task Feed(this ICarnivore animal, Func<Task<IPrey>> getPrey) { ... }
}
```
