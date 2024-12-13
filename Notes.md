Monthly returns and monthly CPI inflation are translated into monthly real returns.
We assume that the retiree has withdrawn an initial amount equal to one-twelfth of the targeted withdrawal rate at the market closing price of the previous month.
The remainder of the portfolio grows at the real market return during the current month.
At the end of the month the retiree withdrawals the next monthly installment and rebalances the portfolio weights to the target equity and bond shares. We assume that the portfolio is subject to a configurable percentage drag from fees for low-cost mutual funds.

Combinations:
start/end dates
number of years
portfolio weights and allocations
withdrawal patterns
final asset target value

RealTR(n) = RealTR(n-1) x [(RealPrice(n)+ Dividend(n)/12)]/RealPrice(n-1)

How can I transform monthly index historical prices into monthly historical real returns. I have monthly historical  prices, dividends, earnings, CPI. An example would be good, using US and S&P 500...