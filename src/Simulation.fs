module Simulation

open Model

module Composition =
    type CompositeStrategySetup =
        {
            NoHistoryStrategy: GameInformation -> Strategy;
            SameColorStrategy: GameInformation -> Strategy;
            DifferentColorStrategy: GameInformation -> Strategy
        }

    let compositeStrategy (setup: CompositeStrategySetup) (info: GameInformation) =
        match (info.History.HasHistory, info.OpponentColor) with
        | (false, _) ->
            setup.NoHistoryStrategy info
        | (true, opponentColor) when opponentColor = info.Agent.Color ->
            setup.SameColorStrategy info
        | _ ->
            setup.DifferentColorStrategy info

module GameMode =
    open Statistics.ModelExtensions

    /// Random Choice game is used in e.g. in stage 2 when euHawk = euDove
    let randomChoiceGame (info: GameInformation): Strategy =
        if info.RandomNumber < 0.5 then // Random number range [0.0, 1.0[
            Dove
        else
            Hawk

    let nashMixedStrategyEquilibriumGameFromPayoffParameters (info: GameInformation) : Strategy =
        let ``change of hawk`` =
            let C = info.PayoffMatrix.``Cost (C)``
            let V = info.PayoffMatrix.``Revard (V)``
            match C with
            | 0.0 -> 1.0
            | _ ->
                let portionOfHawks = V / C
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks

        if (``change of hawk`` > info.RandomNumber) then // Random number range [0.0, 1.0[
            Hawk
        else
            Dove
    let keepSameStrategy (info: GameInformation): Strategy  =
        match info.Agent.Strategy with
        // TODO: Should this be randomm
        | None -> nashMixedStrategyEquilibriumGameFromPayoffParameters info
        | Some choice -> choice

    let highestEuOnDifferentColorGame (info: GameInformation): Strategy =
            let payoff = info.PayoffMatrix
            let lastRound = info.History.LastRoundChallenges
            let opposingColorStats = lastRound.StrategyStatsFor(info.OpponentColor)
            let pHawk = opposingColorStats.HawkPortion // = Hawk count / total actors within color segement
            let pDove = opposingColorStats.DovePortion

            // Caclulate expected payoff for playinf hawk and for playing dove
            // In payoff.GetMyPayoff the first param is my move, and the second is opponent move
            // E.g. for V = 10, C = 20 payoff.GetMyPayoff (Hawk, Hawk) return -5 (= (V-C)/2) and payoff.GetMyPayoff(Dove, Hawk) returns 10 (0)
            let euHawk = pHawk * payoff.GetMyPayoff (Hawk, Hawk) +
                         pDove * payoff.GetMyPayoff (Hawk, Dove)
            let euDove = pHawk * payoff.GetMyPayoff (Dove, Hawk) +
                         pDove * payoff.GetMyPayoff (Dove, Dove)

            match (euHawk - euDove) with
            // When you have expected value for playing hawk and playing dove are equal
            // choose randomly
            | 0.0 -> randomChoiceGame info
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | diff when diff > 0.0 -> Hawk
            | _  -> Dove

// These modes are included in this file temporarily and are not used in the simulation
module ExtraGameModes =
    open Statistics.ModelExtensions
    let nashMixedStrategyEquilibriumGameFromPayoffMatrix (info: GameInformation) : Strategy =
        let ``change of hawk`` =
            let (hawkMax, doveMin) = info.PayoffMatrix.GetPayoffFor(Hawk, Dove)
            let (doveMax, _)       = info.PayoffMatrix.GetPayoffFor(Dove, Dove)
            let (hawkMin, _)       = info.PayoffMatrix.GetPayoffFor(Hawk, Hawk)
            match (hawkMin - doveMin) with
            | 0.0 -> 1.0
            | _ ->
                let hawksPerDove = (doveMax - hawkMax) / (hawkMin - doveMin)
                let portionOfHawks = hawksPerDove / (1.0 + hawksPerDove)
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks

        if (``change of hawk`` > info.RandomNumber) then // Random number range [0.0, 1.0[
            Hawk
        else
            Dove

    let dependingHawksWithinColorSegment (info: GameInformation) =
        let lastRoundStats = info.History.LastRoundChallenges.StrategyStatsFor (info.OpponentColor)
        let hawkPortion = lastRoundStats.HawkPortion
        if (hawkPortion > info.RandomNumber) then // random number range: [0.0, 1.0[
            Hawk
        else
            Dove

    let onHawksOnLastRound (info: GameInformation) =
        let lastRoundStats = info.History.LastRoundChallenges.StrategyStats ()
        let hawkPortion = lastRoundStats.HawkPortion
        if (hawkPortion > info.RandomNumber) then // random number range: [0.0, 1.0[
            Hawk
        else
            Dove


module SimulationStages =

    let stage1Game  = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters

    let stage2Game = Composition.compositeStrategy {
            NoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            DifferentColorStrategy = GameMode.highestEuOnDifferentColorGame
        }

    let stage2GameVersion2 = Composition.compositeStrategy {
            NoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = GameMode.keepSameStrategy
            DifferentColorStrategy = GameMode.highestEuOnDifferentColorGame
        }

    let stage2GameVersion3 = Composition.compositeStrategy {
            NoHistoryStrategy = GameMode.nashMixedStrategyEquilibriumGameFromPayoffParameters
            SameColorStrategy = ExtraGameModes.dependingHawksWithinColorSegment
            DifferentColorStrategy = GameMode.highestEuOnDifferentColorGame
        }


    let stage3Game  = GameMode.keepSameStrategy
