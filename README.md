# CalculatorNotepad

CalculatorNotepad is intended for easy calculations with support for user defined functions/formulas and rich set of integrated mathematical and probability functions.

It can be used as simple calculator with each line showing calculation result, but it also support user variables (storing results of previous calculations and using them in new calculations)
and user defined functions that can be written as simple one line or multiline using integrated script language (or as c# functions in side panel). 

It is also suitable for simple simulation scenarions, with support for random numbers using different distributions and simulation aggregation functions.

![Help screen in CalculatorNotepad](Images/cn_example_help.jpg)

## Overview of supported features
- simple use of math functions, variables and user defined functions 
- support using of units ('kg','in'..), their conversion and user defined units
- support vectors and many functions working on vectors (vec,vDim,vFunc,vSum...)
- provide random generating functions for simulations ( random, rndChoose/Weighted ...)  
- allows easy definition of new user functions using custom language
    - simple single line definitions in the form 'myF(a,b)= 3*a+b'
    - functions can be multiline 'f(a,b)={ ... new lines... }'
    - notepad language support conditions (if/else), loops (for,while), global/local variables
    - most block functions (like if,for,while...) have single-line versions similar to Excel
    - alternative to notepad functions are user defined c# functions 
    - user defined notepad functions can call other user defined notepad or c# functions
    - notepad user functions can be recursive (notepad does auto cache optimization) 
- allow definition of C# user functions (in right C# panel, enabled by 2nd toolbar icon )
    - instantly usable in notepad, allow for complex/faster functions
- Syntax Highlighting of both Notepad and C# panels
    - matching parentheses are highlighted, use Ctrl-Arrows to jump between them
- Autocomplete for Notepad and C# panels
    - Notepad autocomplete also show help/descriptions for builtin custom language functions
    - Ctrl-Space to show all, or automaticaly shown after first typed characters
- Menu (leftmost toolbar icon) allows Load,Save and Options settings
    - Preset file allow permanent user defined functions and constants

## ToDo:  examples for separate features
Until more detailed help for separate notepad features is added here, best way to get additional information about notepad language features is to use 
autocomplete which will automatically appear after first entered letter(s), listing notepad functions starting with those letters and describing their syntax and usage. 
Alternativelly, pressing Ctrl-Space will list all available notepad language functions even without entering first letter. 

