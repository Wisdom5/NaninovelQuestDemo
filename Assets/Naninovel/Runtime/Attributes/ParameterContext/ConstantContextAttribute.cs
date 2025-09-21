// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate specified constant value range.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public class ConstantContextAttribute : ParameterContextAttribute
    {
        public readonly Type EnumType;

        /// <param name="enumType">An enum type to extract constant values from.</param>
        public ConstantContextAttribute (Type enumType, int index = -1, string paramId = null)
            : base(ValueContextType.Constant, enumType.Name, index, paramId)
        {
            if (!enumType.IsEnum) throw new ArgumentException("Only enum types are supported.");
            EnumType = enumType;
        }

        /// <param name="nameExpression">
        /// Expression to evaluate name of the associated constant in IDE.
        /// </param>
        /// <remarks>
        /// Evaluated parts should be enclosed in curly brackets and contain following symbols:
        /// $Script — inspected script name;
        /// :ParameterId — value of the parameter with the specified ID;
        /// :ParameterId[index] — value of named parameter with the specified ID and index;
        /// :ParameterId??... — value of the parameter when assigned or ... (any of the above).
        /// </remarks>
        /// <example>
        /// Labels/{:Path[0]??$Script}
        /// </example>
        public ConstantContextAttribute (string nameExpression, int index = -1, string paramId = null)
            : base(ValueContextType.Constant, nameExpression, index, paramId) { }
    }
}
