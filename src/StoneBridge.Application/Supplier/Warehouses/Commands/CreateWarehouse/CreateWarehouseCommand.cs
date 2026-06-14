using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.CreateWarehouse;

public sealed record CreateWarehouseCommand(CreateWarehouseRequest Request) : IRequest<WarehouseDto>;
