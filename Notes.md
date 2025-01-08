Monthly returns and monthly CPI inflation are translated into monthly real returns.
We assume that the retiree has withdrawn an initial amount equal to one-twelfth of the targeted withdrawal rate at the market closing price of the previous month.
The remainder of the portfolio grwos at the real market return during the curretn month.
At the end of the month the retiree wirthdawals the next monthly installment and rebalances the portfolio weights to the target allocation.
We assume the portfolio is subject to a percentage fee.

End of first month: m1 portfolio value.
Nominal rate for first month: nr1
Inflation rate (CPI) for first month: ir1.
Anual withdrawal rate: wr.
Anual fee: fee

End of first month after withdrawal for second month: m1 - wr/12 * m1
End of second month portfolio value with rebalancing fee: m2 = (m1 - wr/12*m1) (1+nr1)/(1+ir1) * (1 - fee/12)
End of third month portfolio value with rebalancing fee: m3 = (m2 - wr/12*m2) (1+nr2)/(1+ir2) * (1 - fee/12)

Example:
nr|ir|wr|fee|real
0.01|0.002|0.0033|0.00025|100
0.01|0.002|0.0033|0.00025|100.44
where 100.44 = (100 - 0.0033 * 100) (1 + 0.01) / (1 + 0.002) (1 - 0.00025)

Data
Calculate asset returns
- we get monthly historical nominal asset prices as a time series
- normalize the time series by starting at 1
- calculate percentage returns month over month
Calculate inflation returns

Combinations:
start/end dates
number of years
portfolio weights and allocations
withdrawal patterns
final asset target value

RealTR(n) = RealTR(n-1) x [(RealPrice(n)+ Dividend(n)/12)]/RealPrice(n-1)

How can I transform monthly index historical prices into monthly historical real returns. I have monthly historical  prices, dividends, earnings, CPI. An example would be good, using US and S&P 500...
