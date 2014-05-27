/** @jsx React.DOM */ 

var React = require('react');

var WebApp = require('./components/WebApp.react');

React.renderComponent(<WebApp />, document.getElementById('app'));
