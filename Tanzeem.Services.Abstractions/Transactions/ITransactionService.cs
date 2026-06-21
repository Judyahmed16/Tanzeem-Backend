using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Shared.Dtos.Transactions;

namespace Tanzeem.Services.Abstractions.Transactions {
    public  interface ITransactionService {

        // Get
        Task<TransactionDto> GetTransactionByIdAsync(int id);
        Task<IEnumerable<TransactionDto>> GetAllTransactions(int? sortId, int? filterId, string? searchQuery);


        // Post
        Task<int> CreateTransactionAsync(TransactionDto transactionDto);

        Task<int> CreateConfirmOrderTransactionAsync(Order order);

    }
}
