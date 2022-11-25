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
}

resource "azurerm_kusto_database" "otel" {
  name = "otel"
  location = "uksouth"
  resource_group_name = azurerm_resource_group.otel_adx.name
  cluster_name = azurerm_kusto_cluster.cluster.name

  hot_cache_period = "P1D"
  soft_delete_period = "P7D"
}