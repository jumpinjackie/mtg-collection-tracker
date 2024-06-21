using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class ManageVendorsViewModel : DrawerContentViewModel
{
    readonly ICollectionTrackingService _service;

    public ManageVendorsViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
    }

    public ManageVendorsViewModel(ICollectionTrackingService service)
    {
        _service = service;
    }

    [ObservableProperty]
    private string _vendorsText = string.Empty;

    [RelayCommand]
    private async Task Save()
    {
        var input = new ApplyVendorsInputModel
        {
            Names = this.VendorsText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
        };
        await _service.ApplyVendorsAsync(input);
        Messenger.ToastNotify("Vendors updated");
        Messenger.Send(new CloseDrawerMessage());
    }

    public ManageVendorsViewModel WithData(IEnumerable<VendorModel> vendors)
    {
        this.VendorsText = string.Join(Environment.NewLine, vendors.Select(v => v.Name));
        return this;
    }
}
