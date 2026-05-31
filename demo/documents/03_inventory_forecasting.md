# Project: Inventory Demand Forecasting (2024)

> Sample demo document. Fictional Contoso project. Not real.

## Project

A machine-learning service that forecasts product demand for Contoso Insights
retail customers, so small shops can order stock more accurately and reduce both
stockouts and overstock.

## How AI is used

- Trains a time-series forecasting model on historical sales per product.
- Adjusts for seasonality, promotions, and local events.
- Produces a 4-week demand forecast with confidence ranges.

## Tech highlights

- **Frontend:** React dashboard with charts in Contoso Insights.
- **Backend:** Scheduled serverless jobs for training and inference.
- **Data:** Aggregated, anonymized sales data per customer.
- **AI:** Gradient-boosted time-series model, retrained weekly.

## Workflow

1. Nightly job ingests the prior day's anonymized sales.
2. Weekly job retrains the per-product forecasting model.
3. Dashboard shows next-month demand with low/expected/high ranges.
4. Operators export suggested reorder quantities.

## Results

- Pilot customers reported fewer stockouts on top-selling items.
- Overstock on slow movers was reduced in the pilot group.
- Forecasts are advisory: a human always approves reorders.

## Challenges and solutions

- **Sparse data for new products** - solved with category-level fallbacks.
- **Privacy** - solved by training on aggregated, anonymized data only.
