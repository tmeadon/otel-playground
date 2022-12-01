provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "otel_adx" {
  name = "otel-adx"
  location = "uksouth"  
}

resource "azurerm_kusto_cluster" "cluster" {
  name = "tmtest00"
  location = "uksouth"
  resource_group_name = azurerm_resource_group.otel_adx.name

  sku {
    name = "Standard_E4ads_v5"
    capacity = 2
  }

  streaming_ingestion_enabled = true
}

resource "azurerm_kusto_database" "otel" {
  name = "otel"
  location = "uksouth"
  resource_group_name = azurerm_resource_group.otel_adx.name
  cluster_name = azurerm_kusto_cluster.cluster.name

  hot_cache_period = "P1D"
  soft_delete_period = "P7D"
}

resource "azurerm_servicebus_namespace" "sb" {
  name = "oteltest09"
  location  = "uksouth"
  resource_group_name = azurerm_resource_group.otel_adx.name

  sku = "Standard"
}

resource "azurerm_servicebus_queue" "queue1" {
  name = "queue"
  namespace_id = azurerm_servicebus_namespace.sb.id
  enable_partitioning = true 
}

output "sb_connection" {
  value = azurerm_servicebus_namespace.sb.default_primary_connection_string
  sensitive = true
}

output "sb_queue_name" {
  value = azurerm_servicebus_queue.queue1.name
}