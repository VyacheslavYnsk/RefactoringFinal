using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPurchaseService
{
    Task<(IEnumerable<Purchase> Purchases, int TotalCount)> GetByClientAsync(Guid clientId, int page, int size);
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<Purchase> CreateAsync(Guid clientId, PurchaseCreate dto);
    Task<Purchase?> CancelAsync(Guid id, Guid clientId);
}
