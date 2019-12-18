# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.define "rabbitmq" do |rabbitmq|
    rabbitmq.vm.provider "docker" do |docker|
      docker.image = "rabbitmq:3.8-management-alpine"
      docker.ports = ["5672:5672", "15672:15672"]
    end
  end
end
