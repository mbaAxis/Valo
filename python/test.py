import streamlit as st
import pandas as pd
import plotly.express as px

# Sample Complex KPI data
kpi_data = {
    'Revenue': 150000,
    'Profit': 75000,
    'Customers': 1000,
    'Conversion Rate': 0.15,
    'Average Transaction Value': 150,
    'Customer Acquisition Cost': 50,
}

# Title
st.title('Business Performance Dashboard')

# Display KPIs in a more visually appealing way
st.header('Key Performance Indicators (KPIs)')

# Create columns for layout customization
col1, col2 = st.columns(2)

for kpi_name, kpi_value in kpi_data.items():
    if kpi_name in ['Revenue', 'Profit', 'Average Transaction Value']:
        with col1:
            st.metric(label=kpi_name, value=kpi_value)
    elif kpi_name in ['Customers', 'Conversion Rate', 'Customer Acquisition Cost']:
        with col2:
            st.metric(label=kpi_name, value=kpi_value)

# Additional information or visualizations can be added here
st.header('Sales Overview')

# Sample sales data for visualization
sales_data = pd.DataFrame({
    'Month': ['Jan', 'Feb', 'Mar', 'Apr', 'May'],
    'Revenue': [120000, 130000, 110000, 140000, 160000],
    'Profit': [60000, 65000, 55000, 70000, 80000],
})

# Bar chart for monthly revenue and profit
fig = px.bar(sales_data, x='Month', y=['Revenue', 'Profit'], title='Monthly Revenue and Profit Overview')
st.plotly_chart(fig)

# Line chart for conversion rate over time
conversion_data = pd.DataFrame({
    'Month': ['Jan', 'Feb', 'Mar', 'Apr', 'May'],
    'Conversion Rate': [0.12, 0.14, 0.13, 0.16, 0.17],
})

fig_conversion = px.line(conversion_data, x='Month', y='Conversion Rate', title='Conversion Rate Over Time')
st.plotly_chart(fig_conversion)
