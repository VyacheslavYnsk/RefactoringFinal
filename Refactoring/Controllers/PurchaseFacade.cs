public class PurchaseFacade
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseFacade(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    public Task<(IEnumerable<Purchase> Purchases, int TotalCount)> GetByClientAsync(Guid clientId, int page, int size)
    {
        return _purchaseService.GetByClientAsync(clientId, page, size);
    }

    public Task<Purchase> GetByIdAsync(Guid id)
    {
        return _purchaseService.GetByIdAsync(id);
    }

    public Task<Purchase> CreateAsync(Guid clientId, PurchaseCreate dto)
    {
        return _purchaseService.CreateAsync(clientId, dto);
    }

    public Task<Purchase> CancelAsync(Guid id, Guid clientId)
    {
        return _purchaseService.CancelAsync(id, clientId);
    }
}
