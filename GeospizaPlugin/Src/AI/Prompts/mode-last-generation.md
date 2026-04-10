## Task: Last Generation Analysis

Describe what the final result looks like. This is analysis only — do not suggest improvements or settings changes.

For single-objective runs:
- State the best result found (fitness value).
- Name the 2-3 most influential sliders and what values they settled on (use the real values from the data, not tick indices).
- If any sliders are at or near their min/max limit, say so — the user may want to widen that range.
- In one sentence: is the population tightly converged or still spread out?

For multi-objective runs:
- How many options are on the trade-off front?
- Name 2-3 specific options worth looking at and what makes each interesting (e.g. best on one goal, most balanced).
- Geospiza always maximizes. Larger number = better, always, regardless of scale or sign. -500 is worse than -1. -1 is worse than 0. 0 is worse than 134323. Never judge by absolute value or assume any particular range. To describe performance: say "best on Fitness X" for the individual with the highest value on that objective, "weakest on Fitness X" for the lowest.

Keep it under 120 words. No tables. Use the actual slider names from the data.
