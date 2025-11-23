module Main

open Fable.Mocha

Mocha.runTests (testList "All tests" [
    App.Test.tests
    Helpers.Test.tests
    Model.Test.tests
    Simulation.Test.tests
]) |> ignore
