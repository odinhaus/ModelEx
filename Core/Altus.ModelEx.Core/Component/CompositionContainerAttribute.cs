//************************************************************************************************************
//  
//  COPYRIGHT ALTUS SERVICES, LLC 2006, All Rights Reserved     
// 
//
//  Class History
//============================================================================================================
//
//  Developer       Date            Comments
//------------------------------------------------------------------------------------------------------------
//  BILLBL      06/17/2006 14:22:52          [your comments here]
//
//
//
//
//************************************************************************************************************

#region References and Aliases

using System;
using System.Collections.Generic;
using System.Text;

#endregion References and Aliases


namespace Altus.Core.Component
{
    //========================================================================================================//
    /// <summary>
    /// Class name:  ShellAttribute
    /// Class description:
    /// Usage:
    /// <example></example>
    /// <remarks></remarks>
    /// </summary>
    //========================================================================================================//
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public class CompositionContainerAttribute : Attribute
    {
        #region Fields
        #region Static Fields
        #endregion Static Fields

        #region Instance Fields
        private string _loaderType = "";
        private string _shellType = "";
        #endregion Instance Fields
        #endregion Fields

        #region Event Declarations
        #endregion Event Declarations

        #region Constructors
        #region Public
        public CompositionContainerAttribute()
        {
        }
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected

        #endregion  Constructors

        #region Properties
        #region Public

        public string LoaderType
        {
            get { return _loaderType; }
            set { _loaderType = value; }
        }

        public string ShellType
        {
            get { return _shellType; }
            set { _shellType = value; }
        }


        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Properties

        #region Methods
        #region Public
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Methods

        #region Event Handlers and Callbacks
        #region Public
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Event Handlers and Callbacks
    }
}

