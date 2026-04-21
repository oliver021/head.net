import type { SidebarsConfig } from "@docusaurus/plugin-content-docs";

const sidebar: SidebarsConfig = {
  apisidebar: [
    {
      type: "doc",
      id: "api/head-net-sample-api",
    },
    {
      type: "category",
      label: "Invoices",
      items: [
        {
          type: "doc",
          id: "api/list-invoices-paged",
          label: "List invoices (paged)",
          className: "api-method get",
        },
        {
          type: "doc",
          id: "api/create-an-invoice",
          label: "Create an invoice",
          className: "api-method post",
        },
        {
          type: "doc",
          id: "api/get-a-single-invoice",
          label: "Get a single invoice",
          className: "api-method get",
        },
        {
          type: "doc",
          id: "api/update-an-invoice",
          label: "Update an invoice",
          className: "api-method put",
        },
        {
          type: "doc",
          id: "api/delete-an-invoice",
          label: "Delete an invoice",
          className: "api-method delete",
        },
        {
          type: "doc",
          id: "api/pay-an-invoice-custom-action",
          label: "Pay an invoice (custom action)",
          className: "api-method post",
        },
      ],
    },
  ],
};

export default sidebar.apisidebar;
