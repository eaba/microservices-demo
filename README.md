# Hipster Shop: Cloud-Native Microservices Demo Application

This is a fork of the official Google Hipster Shop demo that replaces the Redis backend with a YugabyteDB. The application is a
web-based e-commerce app called **“Hipster Shop”** where users can browse items, add them to the cart, and purchase them.

**Google uses this application to demonstrate use of technologies like
Kubernetes/GKE, Istio, Stackdriver, gRPC and OpenCensus**. This application
works on any Kubernetes cluster (such as a local one), as well as Google
Kubernetes Engine. It’s **easy to deploy with little to no configuration**.

If you’re using this demo, please **★Star** this repository to show your interest!

## Screenshots

| Home Page                                                                                                         | Checkout Screen                                                                                                    |
| ----------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| [![Screenshot of store homepage](./docs/img/hipster-shop-frontend-1.png)](./docs/img/hipster-shop-frontend-1.png) | [![Screenshot of checkout screen](./docs/img/hipster-shop-frontend-2.png)](./docs/img/hipster-shop-frontend-2.png) |

## Service Architecture

**Hipster Shop** is composed of many microservices written in different
languages that talk to each other over gRPC.

[![Architecture of
microservices](./docs/img/architecture-diagram.png)](./docs/img/architecture-diagram.png)

Find **Protocol Buffers Descriptions** at the [`./pb` directory](./pb).

| Service                                              | Language      | Description                                                                                                                       |
| ---------------------------------------------------- | ------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| [frontend](./src/frontend)                           | Go            | Exposes an HTTP server to serve the website. Does not require signup/login and generates session IDs for all users automatically. |
| [cartservice](./src/cartservice)                     | C#            | Stores the items in the user's shopping cart in YugabyteDB and retrieves it.                                                           |
| [productcatalogservice](./src/productcatalogservice) | Go            | Provides the list of products from a JSON file and ability to search products and get individual products.                        |
| [currencyservice](./src/currencyservice)             | Node.js       | Converts one money amount to another currency. Uses real values fetched from European Central Bank. It's the highest QPS service. |
| [paymentservice](./src/paymentservice)               | Node.js       | Charges the given credit card info (mock) with the given amount and returns a transaction ID.                                     |
| [shippingservice](./src/shippingservice)             | Go            | Gives shipping cost estimates based on the shopping cart. Ships items to the given address (mock)                                 |
| [emailservice](./src/emailservice)                   | Python        | Sends users an order confirmation email (mock).                                                                                   |
| [checkoutservice](./src/checkoutservice)             | Go            | Retrieves user cart, prepares order and orchestrates the payment, shipping and the email notification.                            |
| [recommendationservice](./src/recommendationservice) | Python        | Recommends other products based on what's given in the cart.                                                                      |
| [adservice](./src/adservice)                         | Java          | Provides text ads based on given context words.                                                                                   |
| [loadgenerator](./src/loadgenerator)                 | Python/Locust | Continuously sends requests imitating realistic user shopping flows to the frontend.                                              |

## Features

- **[Kubernetes](https://kubernetes.io)/[GKE](https://cloud.google.com/kubernetes-engine/):**
  The app is designed to run on Kubernetes (both locally on "Docker for
  Desktop", as well as on the cloud with GKE).
- **[gRPC](https://grpc.io):** Microservices use a high volume of gRPC calls to
  communicate to each other.
- **[Istio](https://istio.io):** Application works on Istio service mesh *permissive mode only at this time with yugabyte.
- **[OpenCensus](https://opencensus.io/) Tracing:** Most services are
  instrumented using OpenCensus trace interceptors for gRPC/HTTP.
- **[Stackdriver APM](https://cloud.google.com/stackdriver/):** Many services
  are instrumented with **Profiling**, **Tracing** and **Debugging**. In
  addition to these, using Istio enables features like Request/Response
  **Metrics** and **Context Graph** out of the box. When it is running out of
  Google Cloud, this code path remains inactive.
- **[Skaffold](https://skaffold.dev):** Application
  is deployed to Kubernetes with a single command using Skaffold.
- **Synthetic Load Generation:** The application demo comes with a background
  job that creates realistic usage patterns on the website using
  [Locust](https://locust.io/) load generator.

## Installation

We offer two installation methods:

1. **Running locally with “Docker for Desktop”** (~20 minutes) You will build
   and deploy microservices images to a single-node Kubernetes cluster running
   on your development machine.

2. **Running on Google Kubernetes Engine (GKE)”** (~30 minutes) You will build,
   upload and deploy the container images to a Kubernetes cluster on Google
   Cloud.


### Option 1: Running locally with “Docker for Desktop”

> 💡 Recommended if you're planning to develop the application or giving it a
> try on your local cluster.

1. Install tools to run a Kubernetes cluster locally:

   - kubectl (can be installed via `gcloud components install kubectl`)
   - Docker for Desktop (Mac/Windows): It provides Kubernetes support as [noted
     here](https://docs.docker.com/docker-for-mac/kubernetes/).
   - [skaffold]( https://skaffold.dev/docs/install/) (ensure version ≥v0.20)
   - Helm v3 (https://helm.sh/docs/intro/install/)
   - YugabyteDB install local CLI and add to PATH (https://docs.yugabyte.com/latest/quick-start/install/)
   ```sh
   wget https://downloads.yugabyte.com/yugabyte-2.0.10.0-darwin.tar.gz
   tar xvfz yugabyte-2.0.10.0-darwin.tar.gz
   PATH=PATH:yugabyte-2.0.10.0/bin
   ```

1. Launch “Docker for Desktop”. Go to Preferences:

   - choose “Enable Kubernetes”,
   - set CPUs to at least 3, and Memory to at least 6.0 GiB
   - on the "Disk" tab, set at least 32 GB disk space

1. Run `kubectl get nodes` to verify you're connected to “Kubernetes on Docker”.

1. Run `skaffold run` (first time will be slow, it can take ~20 minutes).
   This will build and deploy the application. If you need to rebuild the images
   automatically as you refactor the code, run `skaffold dev` command.

1. Run `kubectl get pods` to verify the Pods are ready and running. The
   application frontend should be available at http://localhost:80 on your
   machine.

### Option 2: Running on Google Kubernetes Engine (GKE)

> 💡 Recommended if you're using Google Cloud Platform and want to try it on
> a realistic cluster.

1.  Install tools specified in the previous section (Docker, kubectl, skaffold, helm, YugabyteDB command line)

1.  Create a Google Kubernetes Engine cluster and make sure `kubectl` is pointing
    to the cluster.

    ```sh
    gcloud services enable container.googleapis.com
    ```

    ```sh
    gcloud container clusters create demo --enable-autoupgrade \
        --enable-autoscaling --min-nodes=3 --max-nodes=10 --num-nodes=5 --zone=us-central1-a
    ```

    ```
    kubectl get nodes
    ```


1.  Prep your cluster and deploy a YugabyteDB cluster via Helm

    ```sh
    kubectl create namespace yb-demo
    kubectl config set-context --namespace yb-demo --current
    helm repo add yugabytedb https://charts.yugabyte.com
    helm repo update
    helm search repo yugabytedb/yugabyte
    helm install yb-demo yugabytedb/yugabyte -f https://raw.githubusercontent.com/YugaByte/charts/master/stable/yugabyte/expose-all.yaml --version 2.0.9 --wait
    ```

1.  Prep your YugabyteDB instance with the following DDL

    ```sh
    bin/ysqlsh -h YOUR_YSQL_IP # default database & user = yugabyte
    \l
    CREATE DATABASE sample;
    \c sample
    CREATE TABLE carts(id serial PRIMARY KEY, userid VARCHAR(50), productid VARCHAR(50), quantity integer);
    ```

1.  Enable Google Container Registry (GCR) on your GCP project and configure the
    `docker` CLI to authenticate to GCR:

    ```sh
    gcloud services enable containerregistry.googleapis.com
    ```

    ```sh
    gcloud auth configure-docker -q
    ```

1.  In the root of this repository, run `skaffold run --default-repo=gcr.io/[PROJECT_ID]`,
    where [PROJECT_ID] is your GCP project ID.

    This command:

    - builds the container images
    - pushes them to GCR
    - applies the `./kubernetes-manifests` deploying the application to
      Kubernetes.

    **Troubleshooting:** If you get "No space left on device" error on Google
    Cloud Shell, you can build the images on Google Cloud Build: [Enable the
    Cloud Build
    API](https://console.cloud.google.com/flows/enableapi?apiid=cloudbuild.googleapis.com),
    then run `skaffold run -p gcb --default-repo=gcr.io/[PROJECT_ID]` instead.

1.  Find the IP address of your application, then visit the application on your
    browser to confirm installation.

        kubectl get service frontend-external

    **Troubleshooting:** A Kubernetes bug (will be fixed in 1.12) combined with
    a Skaffold [bug](https://github.com/GoogleContainerTools/skaffold/issues/887)
    causes load balancer to not to work even after getting an IP address. If you
    are seeing this, run `kubectl get service frontend-external -o=yaml | kubectl apply -f-`
    to trigger load balancer reconfiguration.

### (Work in Progress) Deploying on a Istio-installed GKE cluster

> **Note:** you followed GKE deployment steps above, run `skaffold delete` first
> to delete what's deployed. Enabling Istio-on-GKE sidecar injection may not allow
> previous deployed versions to be re-deployed until it is disabled and removed.

1. Create a GKE cluster and deploy YugabyteDB (described in "Option 2").

1. Use [Istio on GKE add-on](https://cloud.google.com/istio/docs/istio-on-gke/installing)
   to install Istio to your existing GKE cluster.

   ```sh
   gcloud beta container clusters update demo \
       --zone=us-central1-a \
       --update-addons=Istio=ENABLED \
       --istio-config=auth=MTLS_PERMISSIVE
   ```

   > NOTE: MTLS_STRICT is unsupported at this time. If you would like to enable `MTLS_STRICT` mode, you will need to update
   > several manifest files:
   >
   > - `kubernetes-manifests/frontend.yaml`: delete "livenessProbe" and
   >   "readinessProbe" fields.
   > - `kubernetes-manifests/loadgenerator.yaml`: delete "initContainers" field.

1. (Optional) Enable Stackdriver Tracing/Logging with Istio Stackdriver Adapter
   by [following this guide](https://cloud.google.com/istio/docs/istio-on-gke/installing#enabling_tracing_and_logging). Work still needs to be done for Prometheus monitoring to update metrics here: https://cloud.google.com/istio/docs/istio-on-gke/installing

1. Install the automatic sidecar injection (annotate the `yb-demo` namespace
   with the label):

   ```sh
   kubectl label namespace yb-demo istio-injection=enabled
   ```

1. Apply the manifests in [`./istio-manifests`](./istio-manifests) directory.
   (This is required only once.)

   ```sh
   kubectl apply -f ./istio-manifests
   ```

1. Deploy the application with `skaffold run --default-repo=gcr.io/[PROJECT_ID]`.

1. Run `kubectl get pods` to see pods are in a healthy and ready state.

1. Find the IP address of your Istio gateway Ingress or Service, and visit the
   application.

   ```sh
   INGRESS_HOST="$(kubectl -n istio-system get service istio-ingressgateway \
      -o jsonpath='{.status.loadBalancer.ingress[0].ip}')"
   echo "$INGRESS_HOST"
   ```

   ```sh
   curl -v "http://$INGRESS_HOST"
   ```

### Cleanup

If you've deployed the application with `skaffold run` command, you can run
`skaffold delete` to clean up the deployed resources.

If you've deployed the application with `kubectl apply -f [...]`, you can
run `kubectl delete -f [...]` with the same argument to clean up the deployed
resources.

---

This is given as an example deployment of Yugabyte in a microservices environment.
