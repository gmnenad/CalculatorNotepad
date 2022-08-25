# CalculatorNotepad

CaluclatorNotepad is intended for easy calculations with support for user defined functions/formulas and rich set of integrated mathematical and probability functions.

It can be used as simple calculator with each line showing calculation result, but it also support user variables (storing results of previous calculations and using them in new calculations)
and user defin functions that can be written as simple one line or multiline using integrated script language (or as c# functions in side panel). 

It is also suitable for simple simulation scenarions, with support for random numbers using different distributions and simulation aggregation functions.

![Help screen in CalculatorNotepad](Images/cn_example_help.jpg)

## Overview of supported features
- simple use of math functions, variables and user defined functions 
- support using of units ('kg','in'..), their conversion and user defined units
- support vectors and many functions working on vectors (vec,vDim,vFunc,vSum...)
- provide random generating functions for simulations ( random, rndChoose/Weighted ...)  
- allows easy definition of new user functions in single line
    - notepad user functions can be recursive (notepad does auto cache optimization) and multiline {}
- allow definition of C# user functions (in right C# panel, enabled by 2nd toolbar icon )
    - instantly usable in notepad, allow for complex/faster functions
- Syntax Highlighting of both Notepad and C# panels
    - matching parentheses are highlighted, use Ctrl-Arrows to jump between them
- Autocomplete for Notepad and C# panels
    - Notepad autocomplete also show help/descriptions for builtin functions
    - Ctrl-Space to show all, or automaticaly shown after first characters
- Menu (leftmost toolbar icon) allows Load,Save and Options settings
    - Preset file allow permanent user defined functions and constants

## ToDo:  examples for separate features

