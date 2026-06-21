using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Shared.Dtos.Transactions;

namespace Tanzeem.Presentation.Transactions {

    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController(ITransactionService transactionService)
        : ControllerBase {


        [HttpGet]
        [Route("Get-Transaction/{id}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionById(int id) {
            var result = await transactionService.GetTransactionByIdAsync(id);
            return Ok(result);
        }

        [HttpGet]
        [Route("Get-Transactions")]
        [Authorize]
        public async Task<IActionResult> GetTransactions(int? sortId, int? filterId, string? searchQuery) {
            var result = await transactionService.GetAllTransactions(sortId, filterId, searchQuery);
            return Ok(result);
        }

        [HttpPost]
        [Route("Create-Transaction")]
        [Authorize]
        public async Task<IActionResult> CreateTransaction(TransactionDto transaction) {
            var result = await transactionService.CreateTransactionAsync(transaction);
            return Ok(result);
        }


    }
}
