using MediatR;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.CreateSlab;

public sealed record CreateSlabCommand(CreateSlabRequest Request) : IRequest<SupplierSlabDto>;
