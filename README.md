# Sudoku

<b>WIP</b> - Unity based runtime randomly generated Sudoku puzzles


# TODO

- [ ] Improve design (Note: Use of singleton probably fine here since sudoku game isn't likely to expand)
  - [ ] Fix coupling issues
  - [ ] Better readability + commenting
  - [ ] Remove / improve some bloated code
  - [ ] Reduce size of SudokuController class -> spread some functionality to other / new classes (is a bit monolithic!)
  

- [ ] Optimisation (Can take up to 1 sec to make a new 'Hard' game -> Too slow!)
  - [x] Improve game board build time. 
    - Reduced average hard game board build time from ~500ms to ~100ms.
  - [ ] Improve cell value removal algorithm.
  
- [ ] Reduce the minimum number of filled starting cells; with the current algorithm, the minimum (reasonable) starting point is 22/81 filled cells (Average minimum being around 24/81). <b>Note:</b> Not sure how viable this is, as puzzles are randomly generated 

- [ ] Add some kind of seeding functionality, to easily generate the same puzzle again.

- [ ] Add functionality to save / load puzzles from file (Json serialised scriptable objects, maybe?)
