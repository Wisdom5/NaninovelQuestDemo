// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Marks a branch of a conditional execution block,
    /// which is executed in case own condition is met (expression is evaluated to be true), while conditions of the opening [@if] 
    /// and all the preceding [@elseif] (if any) commands are not met.
    /// For usage examples see [conditional execution](/guide/naninovel-scripts#conditional-execution) guide.
    /// </summary>
    public class ElseIf : Command
    {
        /// <summary>
        /// A [script expression](/guide/script-expressions), which should return a boolean value. 
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, ExpressionContext]
        public StringParameter Expression;

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            // We might get here either on exiting from an @if or other @elseif branch (which condition is met), or via direct @goto playback jump. 
            // In any case, we just need to get out of the current conditional block.
            BeginIf.HandleConditionalBlock(true);

            return UniTask.CompletedTask;
        }

        public bool EvaluateExpression () => ExpressionEvaluator.Evaluate<bool>(Expression, LogEvalError);

        private void LogEvalError (string desc = null) => Err($"Failed to evaluate conditional (`@elseif`) expression `{Expression}`. {desc ?? string.Empty}");
    }
}
