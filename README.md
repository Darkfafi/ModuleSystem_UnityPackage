# Module Processor System

The `ModuleProcessor` is a system which processes `ModuleActions` through its Modules. When a module processes the `ModuleAction`, the action is again passed through all modules which have not processed it yet. If nobody reacts to it, the action is resolved.

A `ModuleAction` can chain actions, these will cause the execution stack to grow. And when all these actions have been resolved, only then is the source action resolved.

A `ModuleAction` is a `Request` and `Result`. This means it holds potential data, and executed data. Be sure to read the correct data for your logics!

If you want a method to be executed somewhere in the execution chain of the ModuleSystem, you can chain the `CallbackModuleAction`. This can be used if you wish to chain or do logics only after reading the result caused by the previous actions

This means a `ModuleAction` has the following lifecycle:
1) Enqueued or Chained by a system or module with request data
2) Picked-up by a module to process.
3) Logics executed by module and results injected into the `ModuleAction` (Many times called `MarkAsX` within code base)
4) Requests / Results read by other modules to process, repeat cycle from step 2 when that is the case.

Be sure to follow these rules:
1) Place all modules from `Specific` to `Generic`. So the Specific Modules intercept the actions so the generic fallback are ignored.

## Logics -> Visualization Flow

Have a `ModuleProcessor` which contains all the logics without visualizations, and have it process the request first

Then have another system with a `ModuleProcessor` which listens to the `ActionStackProcessedEvent` of the logics `ModuleProcessor`.

Then have modules which only visualize those actions.

This way, the game still works without visualizations (handy for simulation purposes) and with visualizations (which can be completely overwritten in the waterfall structure of the module system)

Be sure to follow these rules:
1) When you process an action, return `true`, else return `false`. If you don't, the module system will not be able to detect what modules have processed what actions which can lead to unpredictable behaviour.

## DataMap

The `DataMap` is a handy tool which is part of the `ModuleSystem` package which can be used to save small chunks of data inside. This can be used to `Tag` and `Mark` an object. These `Tags` and `Marks` can act as a mediator between `Modules` to communicate and check.

## Debugging

The ModuleSystem has a debug window under `ModuleSystem->TreeView`. This works as following:

1) Drag and drop the appropriate `ActionStackProcessedEvent` which is customly fired by your script. (Recommended to fire it after the `ModuleProcessor.ActionStackProcessedEvent`)
2) Execute Actions within the game
3) When the stack is processed, the actions are added to the view in order of execution with all chained actions included