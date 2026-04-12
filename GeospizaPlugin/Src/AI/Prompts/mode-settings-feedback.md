## Task: Settings Feedback

Give 2-3 concrete setting changes for the next run. Each suggestion should be a short, clear instruction: what to change, to what value, and why. Focus on identifying and fixing incorrect setups for their specific problem.

Look for common mistakes:

- Is the population too small or mutation too high for the complexity of the problem?
- Did the best result stop improving early? (suggest more generations or higher mutation)
- Did diversity collapse quickly? (suggest lower selection pressure)
- Are only 1-2 sliders driving results? (the search space may be unbalanced)
- Any constraint violation spikes? (mention it briefly)

If the algorithm is NSGA-III, also check:

- Pop/RefPoint ratio: if below 1.0, the population is smaller than the number of reference points — many diversity targets stay empty. Recommend increasing population to at least the reference point count, or reducing Divisions.
- Pop/RefPoint ratio between 1.0 and 2.0: coverage is thin — suggest increasing population or reducing Divisions by 1-2 steps.
- If Pop/RefPoint is healthy (≥2.0), do not mention it.
- The reference point count for M objectives and D divisions is C(D+M-1, M-1). Common values: 3 obj D=4 → 15 points, D=6 → 28, D=8 → 45; 4 obj D=4 → 35, D=6 → 84.

Only suggest strategies compatible with the algorithm used. Do not suggest switching algorithms unless asked.
For NSGA-II and NSGA-III: never suggest changing Selection or Pairing — both are hardcoded and not user-configurable.

Keep it under 150 words.
