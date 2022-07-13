﻿// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mzinga.Viewer.ViewModels
{
    public class InformationViewModel : InformationViewModelBase
    {
        public static AppViewModel AppVM
        {
            get
            {
                return AppViewModel.Instance;
            }

        }

        #region Commands

        public RelayCommand Accept
        {
            get
            {
                return _accept ??= new RelayCommand(() =>
                {
                    try
                    {
                        OnRequestClose();
                    }
                    catch (Exception ex)
                    {
                        ExceptionUtils.HandleException(ex);
                    }
                });
            }
        }
        private RelayCommand _accept = null;

        #endregion

        public Action Callback { get; private set; }

        public InformationViewModel(string message, string title, string details = null, Action callback = null) : base(message, title, details)
        {
            Callback = callback;
        }

        public void ProcessClose()
        {
            Callback?.Invoke();
        }
    }
}
